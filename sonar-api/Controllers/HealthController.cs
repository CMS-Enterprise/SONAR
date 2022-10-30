using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snappy;
using Prometheus;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/health")]
public class HealthController : ControllerBase {
  private const String ServiceHealthAggregateMetricName = "sonar_service_status";
  private const String ServiceHealthCheckMetricName = "sonar_service_health_check_status";

  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly ILogger<HealthController> _logger;
  private readonly Uri _prometheusUrl;

  public HealthController(
    IOptions<PrometheusConfiguration> prometheusConfig,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    ILogger<HealthController> logger) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._logger = logger;
    this._prometheusUrl =
      new Uri(
        $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}/api/v1/write"
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
  [HttpPost("{environment}/tenants/{tenant}/services/{service}")]
  [Consumes(typeof(ServiceHealth), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  public async Task<IActionResult> RecordStatus(
    [FromRoute]
    String environment,
    [FromRoute]
    String tenant,
    [FromRoute]
    String service,
    [FromBody]
    ServiceHealth value,
    CancellationToken cancellationToken = default) {

    // Ensure the specified service exists
    await this.FetchExistingService(environment, tenant, service, cancellationToken);

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
        new Label { Name = "environment", Value = environment },
        new Label { Name = "tenant", Value = tenant },
        new Label { Name = "service", Value = service }
      )
    );

    writeData.Timeseries.AddRange(
      value.HealthChecks.SelectMany(kvp =>
        HealthController.CreateHealthStatusMetric(
          HealthController.ServiceHealthCheckMetricName,
          value.Timestamp,
          kvp.Value,
          new Label { Name = "environment", Value = environment },
          new Label { Name = "tenant", Value = tenant },
          new Label { Name = "service", Value = service },
          new Label { Name = "check", Value = kvp.Key }
        )
      )
    );

    using var httpClient = new HttpClient();

    using var buffer = new MemoryStream();
    using var protobufWriter = new CodedOutputStream(buffer);
    writeData.WriteTo(protobufWriter);
    protobufWriter.Flush();

    // Compress
    var compressedData = SnappyCodec.Compress(buffer.ToArray());
    using var compressedBuffer = new MemoryStream(compressedData);

    var response = await httpClient.PostAsync(
      this._prometheusUrl,
      new StreamContent(compressedBuffer) {
        Headers = {
          { "Content-Type", "application/x-protobuf" },
          { "Content-Encoding", "snappy" }
        }
      },
      cancellationToken
    );

    if (!response.IsSuccessStatusCode) {
      var message = await response.Content.ReadAsStringAsync(cancellationToken);

      void HandleErrorMessageAndLog(ProblemDetails problemDetails, LogLevel level) {
        if (!String.IsNullOrWhiteSpace(message)) {
          message = message.Trim();
          problemDetails.Extensions.Add(key: "message", message);
          this._logger.Log(
            level,
            message: "Non-success response from Prometheus ({StatusCode}): {Message}",
            response.StatusCode,
            message
          );
        } else {
          this._logger.Log(
            level,
            message: "Non-success response from Prometheus ({StatusCode})",
            response.StatusCode
          );
        }
      }

      if ((response.StatusCode == HttpStatusCode.BadRequest) && !String.IsNullOrWhiteSpace(message)) {
        var problem = new ProblemDetails {
          Title = "Bad Request",
          Status = 400,
          Detail = "Invalid service health status."
        };
        HandleErrorMessageAndLog(problem, LogLevel.Debug);
      return this.BadRequest(problem);
      } else {
        var problem = new ProblemDetails {
          Title = "Internal Server Error",
          Status = 500,
          Detail = $"Unexpected response from Prometheus ({response.StatusCode})"
        };
        HandleErrorMessageAndLog(problem, LogLevel.Error);
        return this.StatusCode(statusCode: 500, problem);
      }
    }

    return this.NoContent();
  }

  private async Task<Service> FetchExistingService(
    String environmentName,
    String tenantName,
    String serviceName,
    CancellationToken cancellationToken) {

    var results =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenantName),
          leftKeySelector: e => e.Id,
          rightKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new { Environment = env, Tenant = t })
        .LeftJoin(
          this._servicesTable.Where(svc => svc.Name == serviceName),
          leftKeySelector: row => row.Tenant != null ? row.Tenant.Id : (Guid?)null,
          rightKeySelector: svc => svc.TenantId,
          resultSelector: (row, svc) => new {
            Environment = row.Environment,
            Tenant = row.Tenant,
            Service = svc
          })
        .ToListAsync(cancellationToken);

    var result = results.SingleOrDefault();
    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (result.Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    } else if (result.Service == null) {
      throw new ResourceNotFoundException(nameof(Service), serviceName);
    }

    return result.Service;
  }

  private static IEnumerable<TimeSeries> CreateHealthStatusMetric(
    String metricName,
    DateTime timestamp,
    HealthStatus currentStatus,
    params Label[] labels) {

    return Enum.GetValues<HealthStatus>().Select(status => {
      var ts = new TimeSeries {
        Labels = {
          new Label { Name = "__name__", Value = metricName },
          new Label { Name = metricName, Value = status.ToString() }
        },
        Samples = {
          new Sample {
            Timestamp = (Int64)timestamp.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds,
            Value = currentStatus == status ? 1 : 0
          }
        }
      };

      ts.Labels.AddRange(labels);

      return ts;
    });
  }
}
