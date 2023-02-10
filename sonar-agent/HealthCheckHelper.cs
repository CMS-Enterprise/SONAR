using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent;

public class HealthCheckHelper {
  private static readonly Dictionary<(String serviceName, String healthCheck), IImmutableList<(Decimal Timestamp, String Value)>> Cache = new();

  private static readonly HttpHealthCheckCondition DefaultStatusCodeCondition = new StatusCodeCondition(
    new UInt16[] { 200, 204 },
    HealthStatus.Online
  );

  private readonly ILogger<HealthCheckHelper> _logger;

  public HealthCheckHelper(ILogger<HealthCheckHelper> logger) {
    this._logger = logger;
  }

  public async Task RunScheduledHealthCheck(
    TimeSpan interval,
    IConfigurationRoot configRoot,
    ApiConfiguration config,
    PrometheusConfiguration pConfig,
    LokiConfiguration lConfig,
    CancellationToken token) {

    // Configs
    var env = config.Environment;
    var tenant = config.Tenant;
    // SONAR client
    using var sonarHttpClient = new HttpClient();
    sonarHttpClient.Timeout = interval;
    var client = new SonarClient(configRoot, baseUrl: config.BaseUrl, sonarHttpClient);
    await client.ReadyAsync(token);

    // Prometheus client
    using var promHttpClient = new HttpClient();
    promHttpClient.Timeout = interval;
    promHttpClient.BaseAddress = new Uri($"{pConfig.Protocol}://{pConfig.Host}:{pConfig.Port}/");
    var promClient = new PrometheusClient(promHttpClient);
    // Loki Client
    using var lokiHttpClient = new HttpClient();
    lokiHttpClient.Timeout = interval;
    lokiHttpClient.BaseAddress = new Uri($"{lConfig.Protocol}://{lConfig.Host}:{lConfig.Port}/");
    var lokiClient = new LokiClient(lokiHttpClient);

    while (true) {
      if (token.IsCancellationRequested) {
        this._logger.LogInformation("Scheduled health check canceled");
        throw new OperationCanceledException();
      }

      // Get service hierarchy for given env and tenant
      var tenantResult = await client.GetTenantAsync(config.Environment, config.Tenant, token);
      this._logger.LogDebug("Service Count: {ServiceCount}", tenantResult.Services.Count);
      // Iterate over each service
      foreach (var service in tenantResult.Services) {
        // Initialize aggStatus to null
        HealthStatus? aggStatus = null;
        // Get service's health checks here
        var healthChecks = service.HealthChecks;
        var checkResults = new Dictionary<String, HealthStatus>();
        // If no checks are returned, continue
        if ((healthChecks == null) || (healthChecks.Count == 0)) {
          if ((service.Children == null) || (service.Children.Count == 0)) {
            // This service serves no purpose in configuration
            this._logger.LogWarning(
              "No Health Checks or child services associated with {ServiceName}",
              service.Name
            );
          } else {
            this._logger.LogDebug("No Health Checks associated with service {ServiceName}", service.Name);
          }

          continue;
        }

        foreach (var healthCheck in healthChecks) {
          HealthStatus currCheck;

          switch (healthCheck.Type) {
            case HealthCheckType.PrometheusMetric:
              var definition = (PrometheusHealthCheckDefinition)healthCheck.Definition;
              currCheck = await this.RunPrometheusHealthCheck(
                promClient,
                service,
                healthCheck,
                definition,
                token
              );
              break;
            case HealthCheckType.LokiMetric:
              var lokiDefinition = (LokiHealthCheckDefinition)healthCheck.Definition;
              currCheck = await this.RunLokiHealthCheck(
                lokiClient,
                service,
                healthCheck,
                lokiDefinition,
                token
              );
              break;
            case HealthCheckType.HttpRequest:
              var httpDefinition = (HttpHealthCheckDefinition)healthCheck.Definition;
              currCheck = await this.RunHttpHealthCheck(
                service,
                healthCheck,
                httpDefinition,
                timeout: interval,
                token);
              break;
            default:
              throw new NotSupportedException("Healthcheck Type is not supported.");
          }

          // If currCheck is Unknown or currCheck is worse than aggStatus (as long as aggStatus is not Unknown)
          // set aggStatus to currCheck
          if ((currCheck == HealthStatus.Unknown) ||
            ((aggStatus != HealthStatus.Unknown) && (currCheck > (aggStatus ?? 0)))) {
            aggStatus = currCheck;
          }

          // Set checkResults
          checkResults.Add(healthCheck.Name, currCheck);
        }

        // Send result data here
        if (aggStatus != null) {
          await this.SendHealthData(
            env,
            tenant,
            service.Name,
            checkResults,
            client,
            aggStatus.Value,
            token
          );
        }
      }

      await Task.Delay(interval, token);
    }
  }

  private async Task<HealthStatus> RunPrometheusHealthCheck(
    IPrometheusClient promClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    PrometheusHealthCheckDefinition definition,
    CancellationToken token) {

    // Get Prometheus samples
    //  Compute start and end date based on cache

    var end = DateTime.UtcNow;
    var duration = definition.Duration;
    var start = HealthCheckHelper.GetStartDate(service.Name, healthCheck.Name, end, duration);

    ResponseEnvelope<QueryResults> qrResult;
    try {
      qrResult = await promClient.QueryRangeAsync(
        definition.Expression, start, end, TimeSpan.FromSeconds(1), null, token
      );
    } catch (HttpRequestException e) {
      this._logger.LogError(
        e,
        "HTTP error querying Prometheus ({StatusCode}): {Message}",
        e.StatusCode,
        e.Message
      );
      return HealthStatus.Unknown;
    } catch (TaskCanceledException) {
      if (token.IsCancellationRequested) {
        throw;
      } else {
        this._logger.LogError("Prometheus query request timed out");
        return HealthStatus.Unknown;
      }
    } catch (InvalidOperationException e) {
      this._logger.LogError(e, "Unexpected error querying Prometheus: {Message}", e.Message);
      return HealthStatus.Unknown;
    }

    return this.ProcessQueryResults(
      service, healthCheck, definition.Conditions, duration, qrResult);
  }

  private async Task<HealthStatus> RunLokiHealthCheck(
    ILokiClient lokiClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    LokiHealthCheckDefinition definition,
    CancellationToken token) {

    // Set start and end for date range, Get Prometheus samples
    var end = DateTime.UtcNow;
    var duration = definition.Duration;
    var start = HealthCheckHelper.GetStartDate(service.Name, healthCheck.Name, end, duration);

    ResponseEnvelope<QueryResults> qrResult;
    try {
      qrResult = await lokiClient.QueryRangeAsync(
        definition.Expression, start, end, direction: Direction.Forward, cancellationToken: token
      );
    } catch (HttpRequestException e) {
      this._logger.LogError(e, "HTTP error querying Loki: {Message}", e.Message);
      return HealthStatus.Unknown;
    } catch (TaskCanceledException) {
      if (token.IsCancellationRequested) {
        throw;
      } else {
        this._logger.LogError("Loki query request timed out");
        return HealthStatus.Unknown;
      }
    } catch (InvalidOperationException e) {
      this._logger.LogError(e, "Unexpected error querying Loki: {Message}", e.Message);
      return HealthStatus.Unknown;
    }

    return this.ProcessQueryResults(
      service,
      healthCheck,
      definition.Conditions,
      definition.Duration,
      qrResult
    );
  }

  private HealthStatus ProcessQueryResults(
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    IImmutableList<MetricHealthCondition> conditions,
    TimeSpan duration,
    ResponseEnvelope<QueryResults> qrResult) {

    // Error handling
    var currCheck = HealthStatus.Online;
    if (qrResult.Data == null) {
      // No data, bad request
      this._logger.LogWarning("Returned nothing for health check: {HealthCheck}", healthCheck.Name);
      currCheck = HealthStatus.Unknown;
    } else if (qrResult.Data.Result.Count > 1) {
      // Bad config, multiple time series returned
      this._logger.LogWarning(
        "Invalid configuration, multiple time series returned for health check: {HealthCheck}", healthCheck.Name);
      currCheck = HealthStatus.Unknown;
    } else if ((qrResult.Data.Result.Count == 0) ||
      (qrResult.Data.Result[0].Values == null) ||
      (qrResult.Data.Result[0].Values!.Count == 0)) {
      // No samples
      this._logger.LogWarning("Returned no samples for health check: {HealthCheck}", healthCheck.Name);
      currCheck = HealthStatus.Unknown;
    } else {
      // Successfully obtained samples from PromQL, evaluate against all conditions for given check
      var samples = HealthCheckHelper.ComputeCache(qrResult.Data.Result[0].Values!, service.Name, healthCheck.Name, duration.Seconds);

      foreach (var condition in conditions) {
        // Determine which comparison to execute
        // Evaluate all PromQL samples
        var evaluation = HealthCheckHelper.EvaluateSamples(condition.HealthOperator, samples, condition.Threshold);
        // If evaluation is true, set the current check to the condition's status
        // and output to Stdout
        if (evaluation) {
          currCheck = condition.HealthStatus;
          break;
        }
      }
    }

    return currCheck;
  }

  private async Task<HealthStatus> RunHttpHealthCheck(
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    HttpHealthCheckDefinition definition,
    TimeSpan timeout,
    CancellationToken token) {

    try {
      using var handler = new HttpClientHandler {
        AllowAutoRedirect = definition.FollowRedirects != false
      };

      if (definition.SkipCertificateValidation == true) {
        handler.ServerCertificateCustomValidationCallback = (_, _, _, errors) => {
          if (errors != SslPolicyErrors.None) {
            this._logger.LogDebug(
              "Ignoring SSL Certificate Validation Errors ({CertificateErrors}) for Request {Url}",
              String.Join(separator: ", ", Enum.GetValues<SslPolicyErrors>().Where(v => v != SslPolicyErrors.None && errors.HasFlag(v))),
              definition.Url
            );
          }

          return true;
        };
      }

      using var client = new HttpClient(handler);
      client.Timeout = timeout;

      if (definition.AuthorizationHeader != null) {
        if (AuthenticationHeaderValue.TryParse(definition.AuthorizationHeader, out var auth)) {
          client.DefaultRequestHeaders.Authorization = auth;
        } else {
          this._logger.LogWarning(
            "Invalid AuthorizationHeader value for service {Service} health check {HealthCheck}",
            service.Name,
            healthCheck.Name
          );
        }
      }

      // Start out offline
      var currCheck = HealthStatus.Offline;

      // Send request to url specified in definition, calculate duration of request
      var now = DateTime.Now;
      var response = await client.GetAsync(definition.Url, token);
      var duration = DateTime.Now - now;

      // Passed error handling, get status code from response.
      var statusCode = (UInt16)response.StatusCode;

      // Evaluate all status code conditions first
      var statusCodeConditions =
        definition.Conditions
          .Where(c => c.Type == HttpHealthCheckConditionType.HttpStatusCode)
          // If no HttpStatusCode conditions are specified, require the status code to be 200/204
          .DefaultIfEmpty(HealthCheckHelper.DefaultStatusCodeCondition);
      var conditionMet = false;
      foreach (var condition in statusCodeConditions) {
        var statusCondition = (StatusCodeCondition)condition;
        if (statusCondition.StatusCodes.Contains(statusCode)) {
          this._logger.LogDebug(
            "Status code condition {StatusCode} => {ServiceHealth} met for service {Service} health check {HealthCheck}",
            statusCode,
            statusCondition.Status,
            service.Name,
            healthCheck.Name
          );

          currCheck = statusCondition.Status;
          conditionMet = true;
        }
      }

      // If no status code conditions are met, the status will be Offline.
      if (!conditionMet) {
        this._logger.LogDebug(
          "No status code conditions matched {StatusCode} for service {Service} health check {HealthCheck}",
          statusCode,
          service.Name,
          healthCheck.Name
        );

        return HealthStatus.Offline;
      }

      // Evaluate response time conditions. These conditions will only have an effect if the
      // corresponding status is more sever than that based on status code.
      var responseTimeConditions =
        definition.Conditions.Where(c => c.Type == HttpHealthCheckConditionType.HttpResponseTime);
      foreach (var condition in responseTimeConditions) {
        var responseCondition = (ResponseTimeCondition)condition;
        if ((duration > responseCondition.ResponseTime) && (responseCondition.Status > currCheck)) {
          this._logger.LogDebug(
            "Request duration exceeded threshold ({Threshold}) for service {Service} health check {HealthCheck}",
            responseCondition.ResponseTime,
            service.Name,
            healthCheck.Name
          );

          currCheck = responseCondition.Status;
        }
      }

      return currCheck;
    } catch (HttpRequestException e) {
      // Request failed, set currCheck to offline and return.
      this._logger.LogDebug(
        e,
        "Request to {Url} failed for service {Service} health check {HealthCheck}: {Message}",
        definition.Url,
        service.Name,
        healthCheck.Name,
        e.Message
      );

      return HealthStatus.Offline;
    } catch (Exception e) when (e is InvalidOperationException or UriFormatException) {
      // Error with requestURI, this means the configuration is invalid, log and return unknown status.
      this._logger.LogWarning(
        "Invalid Health Check URL: {Url} for service {Service} health check {HealthCheck}",
        definition.Url,
        service.Name,
        healthCheck.Name
      );

      return HealthStatus.Unknown;
    } catch (OperationCanceledException) {
      if (token.IsCancellationRequested) {
        // Task cancelled, raise exception
        throw;
      } else {
        // Http Timeout. Consider the service offline.
        this._logger.LogDebug(
          "Request to {Url} timed out for service {Service} health check {HealthCheck}",
          definition.Url,
          service.Name,
          healthCheck.Name
        );

        return HealthStatus.Offline;
      }
    }
  }

  private async Task SendHealthData(
    String env, String tenant, String service,
    Dictionary<String, HealthStatus> results,
    SonarClient client,
    HealthStatus aggStatus,
    CancellationToken token) {

    var ts = DateTime.UtcNow;
    var healthChecks = new ReadOnlyDictionary<String, HealthStatus>(results);
    var body = new ServiceHealth(ts, aggStatus, healthChecks);

    this._logger.LogInformation(
      "Env: {Environment}, Tenant: {Tenant}, Service: {Service}, Time: {Timestamp}, AggStatus: {AggregateStatus}",
      env,
      tenant,
      service,
      body.Timestamp,
      body.AggregateStatus
    );

    try {
      await client.RecordStatusAsync(env, tenant, service, body, token);
    } catch (ApiException e) {
      this._logger.LogError(
        e,
        "Failed to send status data to SONAR API, Code: {StatusCode}, Message: {Message}",
        e.StatusCode,
        e.Message
      );
    }
  }

  private static DateTime GetStartDate(
    String serviceName,
    String healthCheck,
    DateTime end,
    TimeSpan duration) {

    DateTime start;
    // If no cached values, subtract duration from end date to get start date value.
    //  Else, cached values exist, calculate start date from last cached value.
    if (!HealthCheckHelper.Cache.TryGetValue((serviceName, healthCheck), out var cachedData) ||
      (cachedData.Count == 0)) {
      start = end.Subtract(duration);
    } else {
      start = DateTime.UnixEpoch.AddSeconds((Double)cachedData.Last().Timestamp);
      if (start < end.Subtract(duration)) {
        start = end.Subtract(duration);
      }
    }

    return start;
  }

  private static Boolean EvaluateSamples(
    HealthOperator op, IImmutableList<(Decimal Timestamp, String Value)> values, Decimal threshold) {
    // delegate functions for comparison
    Func<Decimal, Decimal, Boolean> equalTo = (x, y) => x == y;
    Func<Decimal, Decimal, Boolean> notEqual = (x, y) => x != y;
    Func<Decimal, Decimal, Boolean> greaterThan = (x, y) => x > y;
    Func<Decimal, Decimal, Boolean> greaterThanOrEqual = (x, y) => x >= y;
    Func<Decimal, Decimal, Boolean> lessThan = (x, y) => x < y;
    Func<Decimal, Decimal, Boolean> lessThanOrEqual = (x, y) => x <= y;

    Func<Decimal, Decimal, Boolean> comparison;
    switch (op) {
      case HealthOperator.Equal:
        comparison = equalTo;
        break;
      case HealthOperator.NotEqual:
        comparison = notEqual;
        break;
      case HealthOperator.GreaterThan:
        comparison = greaterThan;
        break;
      case HealthOperator.GreaterThanOrEqual:
        comparison = greaterThanOrEqual;
        break;
      case HealthOperator.LessThan:
        comparison = lessThan;
        break;
      case HealthOperator.LessThanOrEqual:
        comparison = lessThanOrEqual;
        break;
      default:
        throw new ArgumentException("Invalid comparison operator.");
    }

    // Iterate through list, if all meet condition, return true, else return false if ANY don't meet condition
    return !values.Any(val => !comparison(Convert.ToDecimal(val.Value), threshold));
  }

  private static IImmutableList<(Decimal Timestamp, String Value)> ComputeCache(
    IImmutableList<(Decimal Timestamp, String Value)> newResults,
    String serviceName,
    String healthCheck,
    Decimal duration) {

    // If cache does not contain key, insert entire response envelope into dictionary.
    //  Else, cache contains service, truncate and concat.
    var key = (serviceName, healthCheck);
    if (!HealthCheckHelper.Cache.TryGetValue(key, out var cachedValues)) {
      HealthCheckHelper.Cache.Add(key, newResults);
    } else {
      var endValue = newResults.Last().Timestamp;
      var beginning = newResults.First().Timestamp;

      // Skip old cached samples that came before the current time window
      var updatedCache = cachedValues.SkipWhile(val => val.Timestamp < (endValue - duration))
        // If there is overlapping data from the cache and the new results, drop the duplicate samples from the cache
        .TakeWhile(d => d.Timestamp < beginning)
        // Concatenate the cached data with the new results
        .Concat(newResults)
        .ToImmutableList();

      HealthCheckHelper.Cache[key] = updatedCache;
    }

    return HealthCheckHelper.Cache[key];
  }
}
