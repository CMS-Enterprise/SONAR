using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
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
  private static readonly Dictionary<String, IImmutableList<(Decimal Timestamp, String Value)>> _cache = new();

  public static async Task RunScheduledHealthCheck(
    TimeSpan interval,
    IConfigurationRoot configRoot,
    ApiConfiguration config,
    PrometheusConfiguration pConfig,
    LokiConfiguration lConfig,
    ILoggerFactory loggerFactory,
    CancellationToken token) {

    // Configs
    var env = config.Environment;
    var tenant = config.Tenant;
    // SONAR client
    using var sonarHttpClient = new HttpClient();
    sonarHttpClient.Timeout = interval;
    var client = new SonarClient(configRoot, baseUrl: config.BaseUrl, sonarHttpClient);
    await client.ReadyAsync(token);
    var i = 0;

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
    // HTTP Metric client
    using var httpMetricClient = new HttpClient();
    httpMetricClient.Timeout = interval;

    var logger = loggerFactory.CreateLogger<HealthCheckHelper>();

    while (true) {
      if (token.IsCancellationRequested) {
        logger.LogInformation("Scheduled health check canceled.");
        throw new OperationCanceledException();
      }

      // Get service hierarchy for given env and tenant
      var tenantResult = await client.GetTenantAsync(config.Environment, config.Tenant, token);
      logger.LogInformation($"Service Count: {tenantResult.Services.Count}");
      // Iterate over each service
      foreach (var service in tenantResult.Services) {
        // Initialize aggStatus to null
        HealthStatus? aggStatus = null;
        // Get service's health checks here
        var healthChecks = service.HealthChecks;
        var checkResults = new Dictionary<String, HealthStatus>();
        // If no checks are returned, log error and continue
        if (healthChecks == null) {
          logger.LogError($"No Health Checks associated with service {service.Name}.");
          continue;
        }

        foreach (var healthCheck in healthChecks) {
          HealthStatus currCheck;

          switch (healthCheck.Type) {
            case HealthCheckType.PrometheusMetric:
              var definition = (PrometheusHealthCheckDefinition)healthCheck.Definition;
              currCheck = await HealthCheckHelper.RunPrometheusHealthCheck(
                promClient,
                service,
                healthCheck,
                definition,
                logger,
                token
              );
              break;
            case HealthCheckType.LokiMetric:
              var lokiDefinition = (LokiHealthCheckDefinition)healthCheck.Definition;
              currCheck = await HealthCheckHelper.RunLokiHealthCheck(
                lokiClient,
                service,
                healthCheck,
                lokiDefinition,
                logger,
                token
              );
              break;
            case HealthCheckType.HttpRequest:
              var httpDefinition = (HttpHealthCheckDefinition)healthCheck.Definition;
              currCheck = await HealthCheckHelper.RunHttpHealthCheck(
                httpMetricClient,
                httpDefinition,
                logger,
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
          await HealthCheckHelper.SendHealthData(
            env,
            tenant,
            service.Name,
            checkResults,
            client,
            aggStatus.Value,
            logger,
            token
          );
        }
      }

      await Task.Delay(interval, token);
      i++;
    }
  }

  private static async Task<HealthStatus> RunPrometheusHealthCheck(
    IPrometheusClient promClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    PrometheusHealthCheckDefinition definition,
    ILogger<HealthCheckHelper> logger,
    CancellationToken token) {

    // Get Prometheus samples
    //  Compute start and end date based on cache

    var end = DateTime.UtcNow;
    var duration = definition.Duration;
    var start = HealthCheckHelper.GetStartDate(service.Name, end, duration);

    ResponseEnvelope<QueryResults> qrResult;
    try {
      qrResult = await promClient.QueryRangeAsync(
        definition.Expression, start, end, TimeSpan.FromSeconds(1), null, token
      );
    } catch (HttpRequestException e) {
      Console.WriteLine($"HttpRequestException: {e.Message}");
      return HealthStatus.Unknown;
    } catch (TaskCanceledException e) {
      if (token.IsCancellationRequested) {
        throw;
      } else {
        Console.WriteLine("Prometheus query request timed out.");
        return HealthStatus.Unknown;
      }
    } catch (InvalidOperationException e) {
      Console.WriteLine($"InvalidOperationException: {e.Message}");
      return HealthStatus.Unknown;
    }

    return HealthCheckHelper.ProcessQueryResults(
      service, healthCheck, definition.Conditions, duration, qrResult, logger);
  }

  private static DateTime GetStartDate(
    String key,
    DateTime end,
    TimeSpan duration) {

    DateTime start;
    // If no cached values, subtract duration from end date to get start date value.
    //  Else, cached values exist, calculate start date from last cached value.
    if (!HealthCheckHelper._cache.ContainsKey(key)) {
      start = end.Subtract(duration);
    } else {
      start = DateTime.UnixEpoch.AddSeconds((Double)HealthCheckHelper._cache[key].Last().Timestamp);
    }

    return start;
  }

  private static async Task<HealthStatus> RunLokiHealthCheck(
    ILokiClient lokiClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    LokiHealthCheckDefinition definition,
    ILogger<HealthCheckHelper> logger,
    CancellationToken token) {

    // Set start and end for date range, Get Prometheus samples
    var end = DateTime.UtcNow;
    var duration = definition.Duration;
    var start = HealthCheckHelper.GetStartDate(service.Name, end, duration);

    ResponseEnvelope<QueryResults> qrResult;
    try {
      qrResult = await lokiClient.QueryRangeAsync(
        definition.Expression, start, end, direction: Direction.Forward, cancellationToken: token
      );
    } catch (HttpRequestException e) {
      Console.WriteLine($"HttpRequestException: {e.Message}");
      return HealthStatus.Unknown;
    } catch (TaskCanceledException e) {
      if (token.IsCancellationRequested) {
        throw;
      } else {
        Console.WriteLine("Loki query request timed out.");
        return HealthStatus.Unknown;
      }
    } catch (InvalidOperationException e) {
      Console.WriteLine($"InvalidOperationException: {e.Message}");
      return HealthStatus.Unknown;
    }

    return HealthCheckHelper.ProcessQueryResults(
      service,
      healthCheck,
      definition.Conditions,
      definition.Duration,
      qrResult,
      logger
    );
  }

  private static HealthStatus ProcessQueryResults(
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    IImmutableList<MetricHealthCondition> conditions,
    TimeSpan duration,
    ResponseEnvelope<QueryResults> qrResult,
    ILogger<HealthCheckHelper> logger) {

    // Error handling
    var currCheck = HealthStatus.Online;
    if (qrResult.Data == null) {
      // No data, bad request
      logger.LogError($"Returned nothing for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else if (qrResult.Data.Result.Count > 1) {
      // Bad config, multiple time series returned
      logger.LogError(
        $"Invalid configuration, multiple time series returned for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else if ((qrResult.Data.Result.Count == 0) ||
      (qrResult.Data.Result[0].Values == null) ||
      (qrResult.Data.Result[0].Values!.Count == 0)) {
      // No samples
      logger.LogError($"Returned no samples for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else {
      // Successfully obtained samples from PromQL, evaluate against all conditions for given check
      var samples = HealthCheckHelper.ComputeCache(qrResult.Data.Result[0].Values!, service, duration.Seconds);

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

  private static async Task<HealthStatus> RunHttpHealthCheck(
    HttpClient client,
    HttpHealthCheckDefinition definition,
    ILogger<HealthCheckHelper> logger,
    CancellationToken token) {

    var currCheck = HealthStatus.Online;

    // Initialize variables needed for Http request and request duration calculation.
    TimeSpan duration;
    HttpResponseMessage response;

    try {
      // Send request to url specified in definition, calculate duration of request
      DateTime now = DateTime.Now;
      response = await client.GetAsync(definition.Url, token);
      duration = DateTime.Now - now;
    } catch (HttpRequestException e) {

      // Request failed, set currCheck to offline and return.
      return HealthStatus.Offline;
    } catch (InvalidOperationException e) {

      // Error with requestURI, log and return unknown status.
      logger.LogError($"Invalid request URI: ${definition.Url}");
      return HealthStatus.Unknown;
    } catch (TaskCanceledException e) {

      // Timeout/task cancelled, return unknown status.
      Console.Error.WriteLine("Request timeout.");
      return HealthStatus.Unknown;
    } catch (UriFormatException e) {

      // Invalid request URI format, log and return unknown status.
      logger.LogError($"Invalid request URI format: {definition.Url}");
      return HealthStatus.Unknown;
    }

    // Passed error handling, get status code from response.
    var statusCode = (UInt16)response.StatusCode;

    // Evaluate response based on conditions
    //  If there is a ResponseTimeCondition, evaluate.
    //  If there is a StatusCodeCondition, evaluate.
    foreach (var condition in definition.Conditions) {
      switch (condition.Type) {
        // Evaluate conditions based on http condition type.
        case HttpHealthCheckConditionType.HttpResponseTime: {
          var responseCondition = (ResponseTimeCondition)condition;
          if (duration > responseCondition.ResponseTime) {
            logger.LogInformation("Request duration exceeded threshold.");
            currCheck = responseCondition.Status;
          }
          break;
        }
        case HttpHealthCheckConditionType.HttpStatusCode: {
          var statusCondition = (StatusCodeCondition)condition;
          if (statusCondition.StatusCodes.Contains(statusCode)) {
            logger.LogInformation($"Request status code {statusCode} met condition.");
            currCheck = statusCondition.Status;
          }
          break;
        }
      }
    }

    return currCheck;
  }

  private static async Task SendHealthData(
    String env, String tenant, String service,
    Dictionary<String, HealthStatus> results,
    SonarClient client,
    HealthStatus aggStatus,
    ILogger<HealthCheckHelper> logger,
    CancellationToken token) {

    var ts = DateTime.UtcNow;
    var healthChecks = new ReadOnlyDictionary<String, HealthStatus>(results);
    var body = new ServiceHealth(ts, aggStatus, healthChecks);

    logger.LogInformation(
      $"Env: {env}, Tenant: {tenant}, Service: {service}, " +
      $"Time: {body.Timestamp}, AggStatus: {body.AggregateStatus}");

    body.HealthChecks.ToList().ForEach(x => Console.WriteLine($"{x.Key}: {x.Value}"));
    try {
      await client.RecordStatusAsync(env, tenant, service, body, token);
    } catch (ApiException e) {
      logger.LogError($"HTTP Request Error, Code: {e.StatusCode}, Message: {e.Message}");
    }
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
    ServiceConfiguration service,
    Decimal duration) {

    // If cache does not contain key, insert entire response envelope into dictionary.
    //  Else, cache contains service, truncate and concat.
    var key = service.Name;
    if (!HealthCheckHelper._cache.ContainsKey(key)) {
      HealthCheckHelper._cache.Add(service.Name, newResults);
    } else {
      var cachedValues = HealthCheckHelper._cache[key];
      var endValue = newResults.Last().Timestamp;
      var beginning = newResults.First().Timestamp;

      // Skip old cached samples that came before the current time window
      cachedValues.SkipWhile(val => val.Timestamp < (endValue - duration))
        // If there is overlapping data from the cache and the new results, drop the duplicate samples from the cache
        .TakeWhile(d => d.Timestamp < beginning)
        // Concatenate the cached data with the new results
        .Concat(newResults)
        .ToImmutableList();

      HealthCheckHelper._cache[key] = cachedValues;
    }

    return HealthCheckHelper._cache[key];
  }
}
