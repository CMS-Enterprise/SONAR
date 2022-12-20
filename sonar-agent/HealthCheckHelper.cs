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

namespace Cms.BatCave.Sonar.Agent;

public class HealthCheckHelper {
  private static Dictionary<String, IImmutableList<(Decimal Timestamp, String Value)>> _cache =
    new Dictionary<string, IImmutableList<(decimal Timestamp, string Value)>>();

    public async Task RunScheduledHealthCheck(
    TimeSpan interval, ApiConfiguration config, PrometheusConfiguration pConfig, LokiConfiguration lConfig, CancellationToken token) {
    // Configs
    var env = config.Environment;
    var tenant = config.Tenant;
    // SONAR client
    var client = new SonarClient(baseUrl: config.BaseUrl, new HttpClient());
    await client.ReadyAsync(token);
    var i = 0;

    // Prometheus client
    using var promHttpClient = new HttpClient();
    promHttpClient.BaseAddress = new Uri($"{pConfig.Protocol}://{pConfig.Host}:{pConfig.Port}/");
    var promClient = new PrometheusClient(promHttpClient);
    // Loki Client
    using var lokiHttpClient = new HttpClient();
    lokiHttpClient.BaseAddress = new Uri($"{lConfig.Protocol}://{lConfig.Host}:{lConfig.Port}/");
    var lokiClient = new LokiClient(lokiHttpClient);
    // HTTP Metric client
    using var httpMetricClient = new HttpClient();
    httpMetricClient.Timeout = TimeSpan.FromSeconds(5);
    // If SIGINT received before interval starts, throw exception
    if (token.IsCancellationRequested) {
      Console.WriteLine("Health check task was cancelled before it got started.");
      throw new OperationCanceledException();
    }

    while (true) {
      if (token.IsCancellationRequested) {
        Console.WriteLine("cancelled");
        throw new OperationCanceledException();
      }

      // Get service hierarchy for given env and tenant
      var tenantResult = await client.GetTenantAsync(config.Environment, config.Tenant, token);
      Console.WriteLine($"Service Count: {tenantResult.Services.Count}");
      // Iterate over each service
      foreach (var service in tenantResult.Services) {
        // Initialize aggStatus to null
        HealthStatus? aggStatus = null;
        // Get service's health checks here
        var healthChecks = service.HealthChecks;
        var checkResults = new Dictionary<String, HealthStatus>();
        // If no checks are returned, log error and continue
        if (healthChecks == null) {
          Console.WriteLine("No Health Checks associated with this service.");
          continue;
        }

        foreach (var healthCheck in healthChecks) {
          HealthStatus currCheck;

          switch (healthCheck.Type) {
            case HealthCheckType.PrometheusMetric:
              var definition = (PrometheusHealthCheckDefinition)healthCheck.Definition;
              currCheck = await RunPrometheusHealthCheck(promClient, service, healthCheck, definition, token);
              break;
            case HealthCheckType.LokiMetric:
              var lokiDefinition = (LokiHealthCheckDefinition)healthCheck.Definition;
              currCheck = await RunLokiHealthCheck(lokiClient, service, healthCheck, lokiDefinition, token);
              break;
            case HealthCheckType.HttpRequest:
              var httpDefinition = (HttpHealthCheckDefinition)healthCheck.Definition;
              currCheck = await RunHttpHealthCheck(
                httpMetricClient,
                httpDefinition,
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
          await SendHealthData(env, tenant, service.Name, checkResults, client, aggStatus ?? HealthStatus.Unknown,
            token);
        }
      }

      Console.WriteLine($"Iteration {i} of health check.");
      await Task.Delay(interval, token);
      i++;
    }
  }

  private static async Task<HealthStatus> RunPrometheusHealthCheck(
    IPrometheusClient promClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    PrometheusHealthCheckDefinition definition,
    CancellationToken token) {

    var currCheck = HealthStatus.Online;
    // Get Prometheus samples
    //  Compute start and end date based on cache

    var end = DateTime.UtcNow;
    var duration = definition.Duration;
    DateTime start = GetStartDate(service.Name, end, duration);

    var qrResult = await promClient.QueryRangeAsync(
      definition.Expression, start, end, TimeSpan.FromSeconds(1), null, token
    );

    return ProcessQueryResults(service, healthCheck, definition.Conditions, duration, qrResult);
  }

  private static DateTime GetStartDate(
    String key,
    DateTime end,
    TimeSpan duration) {

    DateTime start;
    // If no cached values, subtract duration from end date to get start date value.
    //  Else, cached values exist, calculate start date from last cached value.
    if (!_cache.ContainsKey(key)) {
      start = end.Subtract(duration);
    } else {
      DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      start =  UnixEpoch.AddSeconds((double)_cache[key].Last().Timestamp);
    }

    return start;
  }

  private static async Task<HealthStatus> RunLokiHealthCheck(
    ILokiClient lokiClient,
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    LokiHealthCheckDefinition definition,
    CancellationToken token) {

    // Set start and end for date range, Get Prometheus samples
    var end = DateTime.UtcNow;
    var duration = definition.Duration;
    DateTime start = GetStartDate(service.Name, end, duration);

    var qrResult = await lokiClient.QueryRangeAsync(
      definition.Expression, start, end, direction: Direction.Forward, cancellationToken: token
    );

    return ProcessQueryResults(service, healthCheck, definition.Conditions, definition.Duration, qrResult);
  }

  private static HealthStatus ProcessQueryResults(
    ServiceConfiguration service,
    HealthCheckModel healthCheck,
    IImmutableList<MetricHealthCondition> conditions,
    TimeSpan duration,
    ResponseEnvelope<QueryResults> qrResult) {

    // Error handling
    var currCheck = HealthStatus.Online;
    if (qrResult.Data == null) {
      // No data, bad request
      Console.Error.WriteLine($"Returned nothing for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else if (qrResult.Data.Result.Count > 1) {
      // Bad config, multiple time series returned
      Console.Error.WriteLine(
        $"Invalid configuration, multiple time series returned for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else if ((qrResult.Data.Result.Count == 0) ||
               (qrResult.Data.Result[0].Values == null) ||
               (qrResult.Data.Result[0].Values!.Count == 0)) {
      // No samples
      Console.Error.WriteLine($"Returned no samples for health check: {healthCheck.Name}");
      currCheck = HealthStatus.Unknown;
    } else {

      // Successfully obtained samples from PromQL, evaluate against all conditions for given check
      var samples = ComputeCache(qrResult, service, duration.Seconds);

      foreach (var condition in conditions) {
        // Determine which comparison to execute
        // Evaluate all PromQL samples
        var evaluation = EvaluateSamples(condition.HealthOperator, samples!, condition.Threshold);
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
      Console.Error.WriteLine($"Invalid request URI: ${definition.Url}");
      return HealthStatus.Unknown;
    } catch (UriFormatException e) {

      // Invalid request URI format, log and return unknown status.
      Console.Error.WriteLine($"Invalid request URI format: {definition.Url}");
      return HealthStatus.Unknown;
    }

    // Passed error handling, get status code from response.
    var statusCode = (ushort)response.StatusCode;

    // Evaluate response based on conditions
    //  If there is a ResponseTimeCondition, evaluate.
    //  If there is a StatusCodeCondition, evaluate.
    foreach (var condition in definition.Conditions) {
      // Evaluate conditions based on http condition type.
      if (condition.Type == HttpHealthCheckConditionType.HttpResponseTime) {
        var responseCondition = (ResponseTimeCondition)condition;
        if (duration > responseCondition.ResponseTime) {
          Console.WriteLine("Request duration exceeded threshold.");
          currCheck = responseCondition.Status;
        }
      } else if (condition.Type == HttpHealthCheckConditionType.HttpStatusCode) {
        var statusCondition = (StatusCodeCondition)condition;
        if (statusCondition.StatusCodes.Contains(statusCode)) {
          Console.WriteLine($"Request status code {statusCode} met condition.");
          currCheck = statusCondition.Status;
        }
      }
    }

    return currCheck;
  }

  private static async Task SendHealthData(
    String env, String tenant, String service,
    Dictionary<String, HealthStatus> results, SonarClient client, HealthStatus aggStatus, CancellationToken token) {
    var ts = DateTime.UtcNow;
    var healthChecks = new ReadOnlyDictionary<String, HealthStatus>(results);
    ServiceHealth body = new ServiceHealth(ts, aggStatus, healthChecks);

    Console.WriteLine(
      $"Env: {env}, Tenant: {tenant}, Service: {service}, Time: {body.Timestamp}, AggStatus: {body.AggregateStatus}");
    body.HealthChecks.ToList().ForEach(x => Console.WriteLine($"{x.Key}: {x.Value}"));
    try {
      await client.RecordStatusAsync(env, tenant, service, body, token);
    } catch (ApiException e) {
      Console.Error.WriteLine($"HTTP Request Error, Code: {e.StatusCode}, Message: {e.Message}");
    }
  }

  private static Boolean EvaluateSamples(
    HealthOperator op, IImmutableList<(Decimal Timestamp, String Value)> values, Double threshold) {
    // delegate functions for comparison
    Func<Double, Double, Boolean> equalTo = (x, y) => x == y;
    Func<Double, Double, Boolean> notEqual = (x, y) => x != y;
    Func<Double, Double, Boolean> greaterThan = (x, y) => x > y;
    Func<Double, Double, Boolean> greaterThanOrEqual = (x, y) => x >= y;
    Func<Double, Double, Boolean> lessThan = (x, y) => x < y;
    Func<Double, Double, Boolean> lessThanOrEqual = (x, y) => x <= y;

    Func<Double, Double, Boolean> comparison;
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
    return !values.Any(val => !comparison(Convert.ToDouble(val.Value), threshold));
  }

  private static IImmutableList<(Decimal Timestamp, String Value)> ComputeCache(
    ResponseEnvelope<QueryResults> response,
    ServiceConfiguration service,
    Decimal duration) {

    // If cache does not contain key, insert entire response envelope into dictionary.
    //  Else, cache contains service, truncate and concat.
    var newResults = response.Data.Result[0].Values;
    var key = service.Name;
    if (!_cache.ContainsKey(key)) {
      _cache.Add(service.Name, newResults);
    } else {
      var cachedValues = _cache[key];
      var endValue = newResults.Last().Timestamp;

      // Check for duplicate values, remove
      if (newResults.First().Timestamp == cachedValues.Last().Timestamp) {
        cachedValues = cachedValues.RemoveAt(cachedValues.Count - 1);
      }

      // Concat new results to cache
      cachedValues = cachedValues.Concat(newResults).ToImmutableList();
      // Truncate old values
      cachedValues = cachedValues.SkipWhile(val => val.Timestamp < (endValue - duration)).ToImmutableList();
      _cache[key] = cachedValues;
    }

    return _cache[key];
  }
}
