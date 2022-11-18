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

    //Validation
    var canonicalHeathStatusDictionary = await ValidateHealthStatus(environment, tenant, service, value, cancellationToken);

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
      canonicalHeathStatusDictionary.SelectMany(kvp =>
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

    var (httpClient, prometheusClient) = HealthController.GetPrometheusClient(this._prometheusUrl);
    using (httpClient) {
      var serviceStatuses = await this.GetServiceStatuses(
        prometheusClient, environment, tenant, cancellationToken
      );
      var healthCheckStatus = await this.GetHealthCheckStatus(
        prometheusClient, environment, tenant, cancellationToken
      );

      var serviceChildIdsLookup = await this.GetServiceChildIdsLookup(services, cancellationToken);
      var healthChecksByService = await this.GetHealthChecksByService(services, cancellationToken);

      return this.Ok(
        services.Values.Where(svc => svc.IsRootService)
          .Select(svc => HealthController.ToServiceHealth(
            svc, services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
          )
          .ToArray()
      );
    }
  }

  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetSpecificServiceHierarchyHealth")]
  [ProducesResponseType(typeof(ServiceHierarchyHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> GetSpecificServiceHierarchyHealth(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    CancellationToken cancellationToken) {

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceChildIdsLookup = await this.GetServiceChildIdsLookup(services, cancellationToken);

    // Validate specified service
    Service? existingService =
      await this.GetSpecificService(environment, tenant, servicePath, serviceChildIdsLookup, cancellationToken);

    var (httpClient, prometheusClient) = HealthController.GetPrometheusClient(this._prometheusUrl);
    using (httpClient) {
      var serviceStatuses = await this.GetServiceStatuses(
        prometheusClient, environment, tenant, cancellationToken
      );
      var healthCheckStatus = await this.GetHealthCheckStatus(
        prometheusClient, environment, tenant, cancellationToken
      );
      var healthChecksByService = await this.GetHealthChecksByService(services, cancellationToken);

      return this.Ok(HealthController.ToServiceHealth(
        services.Values.Single(svc => svc.Id == existingService.Id),
        services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
      );
    }
  }

  private static (HttpClient, IPrometheusClient) GetPrometheusClient(Uri prometheusUrl) {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = prometheusUrl;
    return (httpClient, new PrometheusClient(httpClient));
  }

  private async Task<Dictionary<String, (DateTime Timestamp, HealthStatus Status)>> GetServiceStatuses(
    IPrometheusClient prometheusClient,
    String environment,
    String tenant,
    CancellationToken cancellationToken) {
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
              StringComparer.OrdinalIgnoreCase
            );

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

    return serviceStatuses;
  }

  private static (String? Service, String? HealthCheck) GetHealthCheckKey(IImmutableDictionary<String, String> labels) {
    labels.TryGetValue(MetricLabelKeys.Service, out var serviceName);
    labels.TryGetValue(MetricLabelKeys.HealthCheck, out var checkName);

    return (serviceName, checkName);
  }

  private async Task<Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)>>
    GetHealthCheckStatus(
      IPrometheusClient prometheusClient,
      String environment,
      String tenant,
      CancellationToken cancellationToken) {
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
                TupleComparer.From(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
              );

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

    return healthCheckStatus;
  }

  private async Task<ILookup<Guid, Guid>> GetServiceChildIdsLookup(
    ImmutableDictionary<Guid, Service> services,
    CancellationToken cancellationToken) {
    var serviceRelationships =
      await this._serviceDataHelper.FetchExistingRelationships(services.Keys, cancellationToken);

    var serviceChildIdsLookup = serviceRelationships.ToLookup(
      keySelector: svc => svc.ParentServiceId,
      elementSelector: svc => svc.ServiceId
    );

    return serviceChildIdsLookup;
  }

  private async Task<Service?> GetSpecificService(
    String environment,
    String tenant,
    String servicePath,
    ILookup<Guid, Guid> serviceChildIds,
    CancellationToken cancellationToken) {

    // Validate root service
    var servicesInPath = servicePath.Split("/");
    var firstService = servicesInPath[0];
    Service existingService =
      await this._serviceDataHelper.FetchExistingService(environment, tenant, firstService, cancellationToken);
    if (!existingService.IsRootService) {
      throw new ResourceNotFoundException(nameof(Service), firstService);
    }

    // If specified service is not root service, validate each subsequent service in given path
    if (servicesInPath.Length > 1) {
      var currParent = existingService;

      foreach (var currService in servicesInPath.Skip(1)) {
        // Ensure current service name matches an existing service
        existingService =
          await this._serviceDataHelper.FetchExistingService(environment, tenant, currService, cancellationToken);

        // Ensure current service is a child of the current parent
        if (!(serviceChildIds[currParent.Id].Contains(existingService.Id))) {
          return null;
        }

        currParent = existingService;
      }
    }

    return existingService;
  }

  private async Task<ILookup<Guid, HealthCheck>> GetHealthChecksByService(
    ImmutableDictionary<Guid, Service> services,
    CancellationToken cancellationToken) {
    var serviceHealthChecks =
      await this._serviceDataHelper.FetchExistingHealthChecks(services.Keys, cancellationToken);

    var healthChecksByService = serviceHealthChecks.ToLookup(hc => hc.ServiceId);

    return healthChecksByService;
  }

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
  private static ServiceHierarchyHealth ToServiceHealth(
    Service service,
    ImmutableDictionary<Guid, Service> services,
    Dictionary<String, (DateTime Timestamp, HealthStatus Status)> serviceStatuses,
    ILookup<Guid, Guid> serviceChildIdsLookup,
    ILookup<Guid, HealthCheck> healthChecksByService,
    Dictionary<(String Service, String HealthCheck), (DateTime Timestamp, HealthStatus Status)> healthCheckStatus) {
    // The service will have its own status if it has health checks that have recorded status.
    var hasServiceStatus = serviceStatuses.TryGetValue(service.Name, out var serviceStatus);

    var children =
      serviceChildIdsLookup[service.Id].Select(sid => ToServiceHealth(
          services[sid], services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus
          )
        )
        .ToImmutableHashSet();

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

  private async Task<IDictionary<String, HealthStatus>> ValidateHealthStatus(String environment, String tenant, String service, ServiceHealth value,
    CancellationToken cancellationToken) {

    // Ensure the specified service exists
    var existingService =
      await this._serviceDataHelper.FetchExistingService(environment, tenant, service, cancellationToken);

    var existingHealthChecks =
      await this._serviceDataHelper.FetchExistingHealthChecks(new []{existingService.Id}, cancellationToken);

    var existingHealthCheckDictionary =
      existingHealthChecks.ToImmutableDictionary(hc => hc.Name, StringComparer.OrdinalIgnoreCase);

    var newHealthStatusByName = new Dictionary<String, HealthStatus>(StringComparer.OrdinalIgnoreCase);
    foreach (var healthCheck in value.HealthChecks) {
      if (existingHealthCheckDictionary.TryGetValue(healthCheck.Key, out var existingHealthCheck)) {
        newHealthStatusByName.Add(existingHealthCheck.Name, healthCheck.Value);
      } else {
        throw new BadRequestException(
          message: "Health Check not present in Configuration",
          new Dictionary<String, Object?> {
            { nameof(HealthCheck), healthCheck.Key }
          }
        );
      }
    }

    return newHealthStatusByName;
  }

  private static class MetricLabelKeys {
    public const String Environment = "environment";
    public const String Tenant = "tenant";
    public const String Service = "service";
    public const String HealthCheck = "check";
  }
}
