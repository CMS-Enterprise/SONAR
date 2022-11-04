using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/health")]
public class HealthController : ControllerBase {
  private const String ServiceHealthAggregateMetricName = "sonar_service_status";
  private const String ServiceHealthCheckMetricName = "sonar_service_health_check_status";

  private readonly PrometheusRemoteWriteClient _remoteWriteClient;
  private readonly ILogger<HealthController> _logger;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly Uri _prometheusUrl;

  public HealthController(
    ServiceDataHelper serviceDataHelper,
    PrometheusRemoteWriteClient remoteWriteClient,
    IOptions<PrometheusConfiguration> prometheusConfig,
    ILogger<HealthController> logger) {

    this._serviceDataHelper = serviceDataHelper;
    this._remoteWriteClient = remoteWriteClient;
    this._logger = logger;
    this._prometheusUrl =
      new Uri(
        $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}"
      );
  }

  /// <summary>
  ///   Records a single health status for the specified service.
  /// </summary>
  /// <remarks>
  ///   Service health status information must be recorded in chronological order per-service, and cannot
  ///   be recorded for timestamps older than 2 hours. Timestamps greater than 2 hours will result in an
  ///   "out of bounds" error. Health status that is reported out of order will result in an "out of
  ///   order sample" error.
  /// </remarks>
  /// <response code="204">The service health status was successfully recorded.</response>
  /// <response code="400">The service health status provided is not valid.</response>
  /// <response code="404">The specified environment, tenant, or service was not found.</response>
  /// <response code="500">An internal error occurred attempting to record the service health status.</response>
  [HttpPost("{environment}/tenants/{tenant}/services/{service}", Name = "RecordStatus")]
  [Consumes(typeof(ServiceHealth), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  public async Task<IActionResult> RecordStatus(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromBody] ServiceHealth value,
    CancellationToken cancellationToken = default) {

    // Ensure the specified service exists
    await this._serviceDataHelper.FetchExistingService(environment, tenant, service, cancellationToken);

    // TODO(BATAPI-95): validate the list of health checks against the service configuration.

    var writeData =
      new WriteRequest {
        Metadata = {
          new MetricMetadata {
            Help = "The aggregate health status of a service.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthController.ServiceHealthAggregateMetricName
          },
          new MetricMetadata {
            Help = "The status of individual service health checks.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthController.ServiceHealthCheckMetricName
          }
        }
      };

    writeData.Timeseries.AddRange(
      HealthController.CreateHealthStatusMetric(
        HealthController.ServiceHealthAggregateMetricName,
        value.Timestamp,
        value.AggregateStatus,
        new Label { Name = MetricLabelKeys.Environment, Value = environment },
        new Label { Name = MetricLabelKeys.Tenant, Value = tenant },
        new Label { Name = MetricLabelKeys.Service, Value = service }
      )
    );

    writeData.Timeseries.AddRange(
      value.HealthChecks.SelectMany(kvp =>
        HealthController.CreateHealthStatusMetric(
          HealthController.ServiceHealthCheckMetricName,
          value.Timestamp,
          kvp.Value,
          new Label { Name = MetricLabelKeys.Environment, Value = environment },
          new Label { Name = MetricLabelKeys.Tenant, Value = tenant },
          new Label { Name = MetricLabelKeys.Service, Value = service },
          new Label { Name = MetricLabelKeys.HealthCheck, Value = kvp.Key }
        )
      )
    );

    var problem = await this._remoteWriteClient.RemoteWriteRequest(writeData, cancellationToken);
    if (problem == null) {
      return this.NoContent();
    }

    return this.StatusCode(problem.Status ?? 500, problem);
  }

  [HttpGet("{environment}/tenants/{tenant}", Name = "GetServiceHierarchyHealth")]
  [ProducesResponseType(typeof(ServiceHierarchyHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetServiceHierarchyHealth(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    CancellationToken cancellationToken) {

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceRelationships =
      await this._serviceDataHelper.FetchExistingRelationships(services.Keys, cancellationToken);
    var serviceHealthChecks =
      await this._serviceDataHelper.FetchExistingHealthChecks(services.Keys, cancellationToken);

    using var httpClient = new HttpClient();
    httpClient.BaseAddress = this._prometheusUrl;
    var prometheusClient = new PrometheusClient(httpClient);

    var serviceStatuses =
      await this.ProcessPrometheusQuery(
        prometheusClient,
        $"{HealthController.ServiceHealthAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
        processResult: results => {
          // StateSet metrics are split into separate metric per-state
          // This code groups all the metrics for a given service and then determines which state is currently set.
          var metricByService =
            results.Result
              .Where(metric => metric.Value.HasValue) // Ignore non-scalar metrics
              .Select(metric => (metric.Labels, metric.Value!.Value))
              .ToLookup(
                keySelector: metric =>
                  metric.Labels.TryGetValue(MetricLabelKeys.Service, out var serviceName) ? serviceName : null,
                StringComparer.OrdinalIgnoreCase);

          var healthMapping =
            new Dictionary<String, (DateTime Timestamp, HealthStatus Status)>(StringComparer.OrdinalIgnoreCase);
          foreach (var group in metricByService) {
            if (group.Key == null) {
              // This group contains metrics that are missing the service name label. These should not exist.
              continue;
            }

            var healthStatus =
              HealthController.GetEffectiveHealthStatus(HealthController.ServiceHealthAggregateMetricName, group);

            if (healthStatus.HasValue) {
              healthMapping[group.Key] = healthStatus.Value;
            }
          }

          return healthMapping;
        },
        cancellationToken
      );

    (String? Service, String? HealthCheck) GetHealthCheckKey(IImmutableDictionary<String, String> labels) {
      labels.TryGetValue(MetricLabelKeys.Service, out var serviceName);
      labels.TryGetValue(MetricLabelKeys.HealthCheck, out var checkName);

      return (serviceName, checkName);
    }

    var healthCheckStatus =
      await this.ProcessPrometheusQuery(
        prometheusClient,
        $"{HealthController.ServiceHealthCheckMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
        processResult: results => {
          // StateSet metrics are split into separate metric per-state
          // This code groups all the metrics for a given state.
          var metricByHealthCheck =
            results.Result
              .Where(metric => metric.Value.HasValue) // Ignore non-scalar metrics
              .Select(metric => (metric.Labels, metric.Value!.Value))
              .ToLookup(
                keySelector: metric => GetHealthCheckKey(metric.Labels),
                TupleComparer.From(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase));

          var healthMapping =
            new Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)>(
              TupleComparer.From<String, String>(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
            );
          foreach (var group in metricByHealthCheck) {
            if ((group.Key.Item1 == null) || (group.Key.Item2 == null)) {
              // This group contains metrics that are missing either the service name label or the
              // metric name label. These should not exist.
              continue;
            }

            var healthStatus =
              HealthController.GetEffectiveHealthStatus(HealthController.ServiceHealthCheckMetricName, group);

            if (healthStatus.HasValue) {
              healthMapping[(group.Key.Item1, group.Key.Item2)] = healthStatus.Value;
            }
          }

          return healthMapping;
        },
        cancellationToken
      );

    var serviceChildIdsLookup =
      serviceRelationships.ToLookup(
        keySelector: svc => svc.ParentServiceId,
        elementSelector: svc => svc.ServiceId);

    var healthChecksByService = serviceHealthChecks.ToLookup(hc => hc.ServiceId);

    // Recursively creates a ServiceHierarchyHealth for the specified service.
    //
    // For each service there are potentially two components that determine its
    // aggregate health status:
    // 1) The health recorded in Prometheus which will be based on the health
    //    checks defined for that service.
    // 2) The aggregate health status of each of its child services.
    //
    // The aggregate health status of a service is always the "worst" of the
    // health check and child service statuses. If *any* health information is
    // missing, then the aggregate health status will be null.
    ServiceHierarchyHealth ToServiceHealth(Service service) {
      // The service will have its own status if it has health checks that have recorded status.
      var hasServiceStatus = serviceStatuses.TryGetValue(service.Name, out var serviceStatus);

      var children =
        serviceChildIdsLookup[service.Id].Select(sid => ToServiceHealth(services[sid])).ToImmutableHashSet();

      HealthStatus? aggregateStatus = hasServiceStatus ? serviceStatus.Status : null;
      DateTime? statusTimestamp = hasServiceStatus ? serviceStatus.Timestamp : null;

      var healthChecks = healthChecksByService[service.Id].ToImmutableList();
      // Only aggregate the status of the children if the current service either
      // has a recorded status based on it's health checks, or has no health
      // checks. (If there is missing information for the health check based
      // service then there is no point considering the status of the children).
      if (hasServiceStatus || !healthChecks.Any()) {
        // If this service has a status
        foreach (var child in children) {
          if (child.AggregateStatus.HasValue) {
            if ((aggregateStatus == null) || (aggregateStatus.Value < child.AggregateStatus.Value)) {
              aggregateStatus = child.AggregateStatus;
            }

            // The child service should always have a timestamp here, but double check anyway
            if (child.Timestamp.HasValue &&
              (!statusTimestamp.HasValue || (child.Timestamp.Value < statusTimestamp.Value))) {

              // The status timestamp should always be the *oldest* of the
              // recorded status data points.
              statusTimestamp = child.Timestamp.Value;
            }
          } else {
            // One of the child services has an "unknown" status, that means
            // this service will also have the "unknown" status.
            aggregateStatus = null;
            statusTimestamp = null;
            break;
          }
        }
      }

      return new ServiceHierarchyHealth(
        service.Name,
        service.DisplayName,
        service.Description,
        service.Url,
        statusTimestamp,
        aggregateStatus,
        healthChecks.ToImmutableDictionary(
          hc => hc.Name,
          hc => healthCheckStatus.TryGetValue((service.Name, hc.Name), out var checkStatus) ?
            checkStatus :
            ((DateTime, HealthStatus)?)null
        ),
        children
      );
    }

    return this.Ok(
      services.Values.Where(svc => svc.IsRootService)
        .Select(ToServiceHealth)
        .ToArray()
    );
  }

  private async Task<T> ProcessPrometheusQuery<T>(
    IPrometheusClient prometheusClient,
    String promQuery,
    Func<QueryResults, T> processResult,
    CancellationToken cancellationToken) {

    var response = await prometheusClient.QueryAsync(
      promQuery,
      DateTime.UtcNow,
      cancellationToken: cancellationToken
    );

    if (response.Status != ResponseStatus.Success) {
      this._logger.LogError(
        message: "Unexpected error querying service health status from Prometheus ({ErrorType}): {ErrorMessage}",
        response.ErrorType,
        response.Error
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying service health status."
      );
    }

    if (response.Data == null) {
      this._logger.LogError(
        message: "Prometheus unexpectedly returned null data for query {Query}",
        promQuery
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying service health status."
      );
    }

    return processResult(response.Data);
  }

  private static (DateTime timestamp, HealthStatus status)? GetEffectiveHealthStatus(
    String metricName,
    IEnumerable<(IImmutableDictionary<String, String> Labels, (Decimal Timestamp, String Flag) Value)> group) {

    (Decimal Timestamp, HealthStatus Status)? current = null;
    foreach (var entry in group) {
      // Filter to only the metrics that have the flag set, and ignore any metrics then do not have
      // a valid HealthStatus label.
      if ((entry.Value.Flag == "1") &&
        entry.Labels.TryGetValue(metricName, out var statusStr) &&
        Enum.TryParse<HealthStatus>(statusStr, out var status)) {

        if (current == null) {
          current = (entry.Value.Timestamp, status);
        } else if (entry.Value.Timestamp > current.Value.Timestamp) {
          // If there are multiple statuses with the flag set, use the newest one.
          current = (entry.Value.Timestamp, status);
        } else if ((entry.Value.Timestamp == current.Value.Timestamp) && (status > current.Value.Status)) {
          // If there are multiple statuses with the flag set and the same timestamp, use the most severe
          current = (entry.Value.Timestamp, status);
        }
      }
    }

    return current.HasValue ?
      (DateTime.UnixEpoch.AddMilliseconds((Int64)(current.Value.Timestamp * 1000)), current.Value.Status) :
      null;
  }

  private static IEnumerable<TimeSeries> CreateHealthStatusMetric(
    String metricName,
    DateTime timestamp,
    HealthStatus currentStatus,
    params Label[] labels) {

    var timestampMs = (Int64)timestamp.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;

    return Enum.GetValues<HealthStatus>().Select(status => {
      var ts = new TimeSeries {
        Labels = {
          new Label { Name = "__name__", Value = metricName },
          new Label { Name = metricName, Value = status.ToString() }
        },
        Samples = {
          new Sample {
            Timestamp = timestampMs,
            Value = currentStatus == status ? 1 : 0
          }
        }
      };

      ts.Labels.AddRange(labels);

      return ts;
    });
  }

  private static class MetricLabelKeys {
    public const String Environment = "environment";
    public const String Tenant = "tenant";
    public const String Service = "service";
    public const String HealthCheck = "check";
  }
}
