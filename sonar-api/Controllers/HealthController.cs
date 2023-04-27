using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
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
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/health")]
public class HealthController : ControllerBase {
  private readonly PrometheusRemoteWriteClient _remoteWriteClient;
  private readonly ILogger<HealthController> _logger;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly CacheHelper _cacheHelper;
  private readonly String _sonarEnvironment;
  private readonly DataContext _dbContext;
  private readonly IOptions<DatabaseConfiguration> _dbConfig;
  private readonly Uri _prometheusUrl;

  public HealthController(
    DataContext dbContext,
    ServiceDataHelper serviceDataHelper,
    ApiKeyDataHelper apiKeyDataHelper,
    HealthDataHelper healthDataHelper,
    CacheHelper cacheHelper,
    PrometheusRemoteWriteClient remoteWriteClient,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig,
    IOptions<DatabaseConfiguration> dbConfig,
    IOptions<PrometheusConfiguration> prometheusConfig,
    ILogger<HealthController> logger) {

    this._dbContext = dbContext;
    this._serviceDataHelper = serviceDataHelper;
    this._apiKeyDataHelper = apiKeyDataHelper;
    this._healthDataHelper = healthDataHelper;
    this._cacheHelper = cacheHelper;
    this._remoteWriteClient = remoteWriteClient;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
    this._dbConfig = dbConfig;
    this._prometheusUrl = new Uri(
      $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}/"
    );
    this._logger = logger;
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
    var canonicalHeathStatusDictionary =
      await ValidateHealthStatus(environment, tenant, service, value, cancellationToken);

    // local function to handle exception handling for caching
    async Task CachingTaskExceptionHandling() {
      try {
        await this._cacheHelper
          .CreateUpdateCache(
            environment,
            tenant,
            service,
            value,
            canonicalHeathStatusDictionary.ToImmutableDictionary(),
            cancellationToken);
      } catch (Exception e) {
        this._logger.LogError(
          message: "Unexpected error occurred during caching process: {Message}",
          e.Message
        );
      }
    }

    var writeData =
      new WriteRequest {
        Metadata = {
          new MetricMetadata {
            Help = "The aggregate health status of a service.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthDataHelper.ServiceHealthAggregateMetricName
          },
          new MetricMetadata {
            Help = "The status of individual service health checks.",
            Type = MetricMetadata.Types.MetricType.Stateset,
            MetricFamilyName = HealthDataHelper.ServiceHealthCheckMetricName
          }
        }
      };

    writeData.Timeseries.AddRange(
      HealthController.CreateHealthStatusMetric(
        HealthDataHelper.ServiceHealthAggregateMetricName,
        value.Timestamp,
        value.AggregateStatus,
        new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
        new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service }
      )
    );

    writeData.Timeseries.AddRange(
      canonicalHeathStatusDictionary.SelectMany(kvp =>
        HealthController.CreateHealthStatusMetric(
          HealthDataHelper.ServiceHealthCheckMetricName,
          value.Timestamp,
          kvp.Value,
          new Label { Name = HealthDataHelper.MetricLabelKeys.Environment, Value = environment },
          new Label { Name = HealthDataHelper.MetricLabelKeys.Tenant, Value = tenant },
          new Label { Name = HealthDataHelper.MetricLabelKeys.Service, Value = service },
          new Label { Name = HealthDataHelper.MetricLabelKeys.HealthCheck, Value = kvp.Key }
        )
      )
    );

    // cache data in parallel with Prometheus request
    var cachingTask = CachingTaskExceptionHandling();
    var prometheusTask = this._remoteWriteClient.RemoteWriteRequest(writeData, cancellationToken);

    try {
      await Task.WhenAll(new[] { cachingTask, prometheusTask });
    } catch (AggregateException e) {
      foreach (var ie in e.InnerExceptions) {
        this._logger.LogError(
          message: "Unexpected error ({ExceptionType}): {Message}",
          ie.GetType(),
          ie.Message
        );
      }
    }

    ProblemDetails? problem = await prometheusTask;

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

    var postgresCheck = await this.RunPostgresHealthCheck(cancellationToken);
    var prometheusCheck = await this.RunPrometheusSelfCheck(cancellationToken);
    var result = new List<ServiceHierarchyHealth>() { postgresCheck, prometheusCheck };
    return this.Ok(result);
  }

  private async Task<ServiceHierarchyHealth> RunPostgresHealthCheck(CancellationToken cancellationToken) {
    var aggStatus = HealthStatus.Online;
    var healthChecks =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    var connectionTestResult = HealthStatus.Online;
    var sonarDbTestResult = HealthStatus.Online;

    try {
      await _dbContext.Database.OpenConnectionAsync(cancellationToken: cancellationToken);
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
        $"postgresql://{_dbConfig.Value.Host}:{_dbConfig.Value.Port}/{_dbConfig.Value.Database}"),
      DateTime.UtcNow,
      aggStatus,
      healthChecks.ToImmutableDictionary(),
      null
    );
  }

  private async Task<ServiceHierarchyHealth> RunPrometheusSelfCheck(CancellationToken cancellationToken) {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = this._prometheusUrl;
    var healthChecks =
      new Dictionary<String, (DateTime Timestamp, HealthStatus Status)?>();
    var readinessTest = HealthStatus.Online;
    var queryTest = HealthStatus.Online;

    try {
      await httpClient.GetAsync(
        "-/ready",
        cancellationToken);
    } catch (HttpRequestException e) {
      // Failed readiness probe
      readinessTest = HealthStatus.Offline;
      queryTest = HealthStatus.Unknown;
    } catch (Exception e) {
      // Unknown exception
      readinessTest = HealthStatus.Unknown;
      queryTest = HealthStatus.Unknown;
    }

    healthChecks.Add("readiness-probe", (DateTime.UtcNow, readinessTest));
    healthChecks.Add("test-query", (DateTime.UtcNow, queryTest));

    // calculate aggStatus
    var aggStatus = new[] { readinessTest, queryTest }.Max();

    return new ServiceHierarchyHealth(
      "prometheus",
      "Prometheus",
      "The Prometheus instance that the SONAR API uses to persist service health information.",
      this._prometheusUrl,
      DateTime.UtcNow,
      aggStatus,
      healthChecks.ToImmutableDictionary(),
      Children: null
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

    var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(
      environment, tenant, cancellationToken
    );
    var healthCheckStatus = await this._healthDataHelper.GetHealthCheckStatus(
      environment, tenant, cancellationToken
    );

    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

    return this.Ok(
      services.Values.Where(svc => svc.IsRootService)
        .Select(svc => this._healthDataHelper.ToServiceHealth(
          svc, services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
        )
        .ToArray()
    );
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
      await this._serviceDataHelper.GetSpecificService(environment, tenant, servicePath, serviceChildIdsLookup,
        cancellationToken);

    if (existingService == null) {
      throw new ResourceNotFoundException(nameof(Service), servicePath);
    }

    var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(
      environment, tenant, cancellationToken
    );
    var healthCheckStatus = await this._healthDataHelper.GetHealthCheckStatus(
      environment, tenant, cancellationToken
    );
    var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);

    return this.Ok(this._healthDataHelper.ToServiceHealth(
      services.Values.Single(svc => svc.Id == existingService.Id),
      services, serviceStatuses, serviceChildIdsLookup, healthChecksByService, healthCheckStatus)
    );
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

  private async Task<IDictionary<String, HealthStatus>> ValidateHealthStatus(
    String environment, String tenant, String service, ServiceHealth value,
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
}
