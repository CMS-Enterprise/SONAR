using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
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
using Cms.BatCave.Sonar.Query;
using Cms.BatCave.Sonar.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Prometheus;
using Enum = System.Enum;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/health")]
public class HealthController : ControllerBase {
  public const String ServiceHealthAggregateMetricName = "sonar_service_status";
  public const String ServiceHealthCheckMetricName = "sonar_service_health_check_status";

  // When querying for the services current health, the maximum age of data
  // points from Prometheus to consider. If there are no data points newer than
  // this the services health status will be unknown.
  private static readonly TimeSpan MaximumServiceHealthAge = TimeSpan.FromHours(1);

  private readonly PrometheusRemoteWriteClient _remoteWriteClient;
  private readonly ILogger<HealthController> _logger;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly Uri _prometheusUrl;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;
  private readonly String _sonarEnvironment;
  private readonly DataContext _dbContext;
  private readonly IOptions<DatabaseConfiguration> _dbConfig;

  public HealthController(
    ServiceDataHelper serviceDataHelper,
    PrometheusRemoteWriteClient remoteWriteClient,
    IOptions<PrometheusConfiguration> prometheusConfig,
    ILogger<HealthController> logger,
    ApiKeyDataHelper apiKeyDataHelper,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig,
    DataContext dbContext,
    IOptions<DatabaseConfiguration> dbConfig) {

    this._serviceDataHelper = serviceDataHelper;
    this._remoteWriteClient = remoteWriteClient;
    this._logger = logger;
    this._prometheusUrl =
      new Uri(
        $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}"
      );
    this._apiKeyDataHelper = apiKeyDataHelper;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
    this._dbContext = dbContext;
    this._dbConfig = dbConfig;

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
  /// <response code="401">The API key in the header is not authorized for recording.</response>
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
    await this._apiKeyDataHelper.ValidateTenantPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(),
      environment,
      tenant,
      "record a health status",
      cancellationToken);
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

    if (problem.Status == (Int32)HttpStatusCode.BadRequest) {
      problem.Type = ProblemTypes.InvalidData;
    }
    return this.StatusCode(problem.Status ?? (Int32)HttpStatusCode.InternalServerError, problem);
  }

  [HttpGet("{environment}/tenants/sonar", Name = "GetSonarHealth")]
  [ProducesResponseType(typeof(ServiceHierarchyHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  public async Task<IActionResult> GetSonarHealth(
    [FromRoute] String environment,
    CancellationToken cancellationToken) {

    // Check if environment provided matches value in config.
    if (environment != this._sonarEnvironment) {
      return this.NotFound(new {
        Message = "Sonar environment not found."
      });
    }

    var postgresCheck = await RunPostgresHealthCheck();
    var result = new List<ServiceHierarchyHealth>() { postgresCheck };
    return this.Ok(result);
  }

  private async Task<ServiceHierarchyHealth> RunPostgresHealthCheck() {
    var aggStatus = HealthStatus.Online;
    var healthChecks =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    var connectionTestResult = HealthStatus.Online;
    var sonarDbTestResult = HealthStatus.Online;

    try {
      await _dbContext.Database.OpenConnectionAsync();
    } catch (InvalidOperationException e) {
      // Db connection issue
      connectionTestResult = HealthStatus.Offline;
      sonarDbTestResult = HealthStatus.Unknown;
    } catch (PostgresException e) {
      // Sonar db issue
      sonarDbTestResult = HealthStatus.Offline;
    }

    healthChecks.Add("connection-test", (DateTime.UtcNow, connectionTestResult));
    healthChecks.Add("sonar-database-test", (DateTime.UtcNow, sonarDbTestResult));

    // calculate aggStatus
    aggStatus = new[] { connectionTestResult, sonarDbTestResult }.Max();

    return new ServiceHierarchyHealth(
      "postgresql",
      "Postgresql",
      "The Postgresql instance that the SONAR API uses to persist service health information.",
      new Uri(
        $"postgresql://{_dbConfig.Value.Username}@{_dbConfig.Value.Host}:{_dbConfig.Value.Port}"),
      DateTime.UtcNow,
      aggStatus,
      healthChecks.ToImmutableDictionary(),
      null
    );
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

      var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
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
    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);

    // Validate specified service
    Service? existingService =
      await this._serviceDataHelper.GetSpecificService(environment, tenant, servicePath, serviceChildIdsLookup, cancellationToken);

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
    var serviceStatuses = await this.GetLatestValuePrometheusQuery(
      prometheusClient,
      $"{HealthController.ServiceHealthAggregateMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
      processResult: results => {
        // StateSet metrics are split into separate metric per-state
        // This code groups all the metrics for a given service and then determines which state is currently set.
        var metricByService =
          results.Result
            .Where(metric => metric.Values != null)
            .Select(metric => (metric.Labels, metric.Values!.OrderByDescending(v => v.Timestamp).FirstOrDefault()))
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
      await this.GetLatestValuePrometheusQuery(
        prometheusClient,
        $"{HealthController.ServiceHealthCheckMetricName}{{environment=\"{environment}\", tenant=\"{tenant}\"}}",
        processResult: results => {
          // StateSet metrics are split into separate metric per-state
          // This code groups all the metrics for a given state.
          var metricByHealthCheck =
            results.Result
              .Where(metric => metric.Values != null)
              .Select(metric => (metric.Labels, metric.Values!.OrderByDescending(v => v.Timestamp).FirstOrDefault()))
              .ToLookup(
                keySelector: metric => HealthController.GetHealthCheckKey(metric.Labels),
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
    // has a recorded status based on its health checks, or has no health
    // checks. (If there is missing information for the health check based
    // service then there is no point considering the status of the children).
    if (hasServiceStatus || !healthChecks.Any()) {
      // If this service has a status
      foreach (var child in children) {
        if (child.AggregateStatus.HasValue) {
          if ((aggregateStatus == null) ||
            (aggregateStatus.Value < child.AggregateStatus.Value) ||
            (child.AggregateStatus == HealthStatus.Unknown)) {

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

  private async Task<T> GetLatestValuePrometheusQuery<T>(
    IPrometheusClient prometheusClient,
    String promQuery,
    Func<QueryResults, T> processResult,
    CancellationToken cancellationToken) {

    var response = await prometheusClient.QueryAsync(
      // metric{tag="value"}[time_window]
      $"{promQuery}[{PrometheusClient.ToPrometheusDuration(HealthController.MaximumServiceHealthAge)}]",
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
      await this._serviceDataHelper.FetchExistingHealthChecks(new[] { existingService.Id }, cancellationToken);

    var existingHealthCheckDictionary =
      existingHealthChecks.ToImmutableDictionary(hc => hc.Name, StringComparer.OrdinalIgnoreCase);

    var newHealthStatusByName = new Dictionary<String, HealthStatus>(StringComparer.OrdinalIgnoreCase);
    foreach (var healthCheck in value.HealthChecks) {
      if (existingHealthCheckDictionary.TryGetValue(healthCheck.Key, out var existingHealthCheck)) {
        newHealthStatusByName.Add(existingHealthCheck.Name, healthCheck.Value);
      } else {
        throw new BadRequestException(
          message: "Health Check not present in Configuration",
          ProblemTypes.InconsistentData,
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
