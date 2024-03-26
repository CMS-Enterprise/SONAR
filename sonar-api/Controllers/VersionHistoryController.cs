using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using PrometheusQuerySdk;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/version-history")]
public class VersionHistoryController : ControllerBase {

  private readonly PrometheusQueryHelper _prometheusQueryHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly VersionDataHelper _versionDataHelper;

  public VersionHistoryController(
    PrometheusQueryHelper prometheusQueryHelper,
    ServiceDataHelper serviceDataHelper,
    VersionDataHelper versionDataHelper) {

    this._prometheusQueryHelper = prometheusQueryHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._versionDataHelper = versionDataHelper;
  }

  /// <summary>
  ///   Get the version history for all services within the specified Tenant.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="duration">How far back in time values should be fetched (in seconds).</param>
  /// <param name="timeQuery">The timestamp at which to sample data.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">Successfully retrieved the version history for all the Tenant's services.</response>
  /// <response code="400">The given timestamp was not expressed in UTC.</response>
  /// <response code="404">The specified environment or tenant does not exist.</response>
  [HttpGet("{environment}/tenants/{tenant}", Name = "GetServicesVersionHistory")]
  [ProducesResponseType(typeof(ServiceVersionHistory[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<IActionResult> GetServicesVersionHistory(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromQuery] Int32? duration,
    [FromQuery] DateTime? timeQuery,
    CancellationToken cancellationToken) {

    var (queryRange, queryTimestamp) = ValidateQueryParameters(duration, timeQuery);

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var servicesVersions =
      await this.GetServicesVersionsHistory(
        environment,
        tenant,
        queryRange,
        queryTimestamp,
        cancellationToken);

    var servicesVersionHistory = new List<ServiceVersionHistory>();
    foreach (var serviceInfo in services) {
      var service = serviceInfo.Value;
      if (servicesVersions.TryGetValue(service.Name, out var serviceVersion)) {
        var history = serviceVersion?.ToImmutableList();
        servicesVersionHistory.Add(
          new ServiceVersionHistory(
            service.Name,
            service.DisplayName,
            service.Description,
            service.Url,
            history
          ));
      }
    }

    return this.Ok(servicesVersionHistory);
  }

  /// <summary>
  ///   Get the version history for a specific Service, specified by its path in the service hierarchy.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="servicePath">The path to the service in the service hierarchy.</param>
  /// <param name="duration">How far back in time values should be fetched (in seconds).</param>
  /// <param name="timeQuery">The timestamp at which to sample data.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">Successfully retrieved the version history for the specified Service.</response>
  /// <response code="400">The given timestamp was not expressed in UTC.</response>
  /// <response code="404">The specified environment, tenant, or service does not exist.</response>
  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetServiceVersionHistory")]
  [ProducesResponseType(typeof(ServiceVersionHistory), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<IActionResult> GetServiceVersionHistory(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    [FromQuery] Int32? duration,
    [FromQuery] DateTime? timeQuery,
    CancellationToken cancellationToken) {

    var (queryRange, queryTimestamp) = ValidateQueryParameters(duration, timeQuery);

    var (_, _, services) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
    var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
    var existingService =
      await this._serviceDataHelper.GetSpecificService(
        environment, tenant, servicePath, serviceChildIdsLookup, cancellationToken);

    if (existingService == null) {
      throw new ResourceNotFoundException(nameof(Service), servicePath);
    }

    var serviceVersions =
      await this.GetServiceVersionsHistory(
        environment,
        tenant,
        existingService.Name,
        queryRange,
        queryTimestamp,
        cancellationToken);

    return this.Ok(new ServiceVersionHistory(
      existingService.Name,
      existingService.DisplayName,
      existingService.Description,
      existingService.Url,
      serviceVersions
      ));
  }

  private async Task<Dictionary<String, IImmutableList<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>>>
    GetServicesVersionsHistory(
      String environment,
      String tenant,
      String range,
      DateTime timeQuery,
      CancellationToken cancellationToken) {

    String promQuery = $"{VersionDataHelper.ServiceVersionAggregateMetricName}" +
      $"{{environment=\"{environment}\", tenant=\"{tenant}\"}}" +
      $"[{range}]";

    return await this._prometheusQueryHelper.GetInstantaneousValuePromQuery(
      promQuery,
      timeQuery,
      processResult: results => {

        // Group metrics by Service
        var versionValuesByService = results.Result
          .Where(series => (series.Values != null) && series.Values.Any())
          .Select(series => (
            series.Labels,
            series.Values!
              .OrderByDescending(versionInfo => versionInfo.Timestamp)
              .ToList()))
          .ToLookup(
            keySelector: series =>
              series.Labels.TryGetValue("service", out var serviceName) ?
                serviceName :
                null, StringComparer.OrdinalIgnoreCase);

        var serviceVersionHistoryMap = new Dictionary<String, IImmutableList<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>>();

        // Iterate through Services and create version time series
        foreach (var service in versionValuesByService) {
          if (service.Key is null) {
            throw new InvalidOperationException("The time series for version is missing the service label");
          }

          var serviceName = service.Key;
          var versionsByTimestamp = new List<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>();

          // Order Service version metrics by their timestamp
          var timestampedVersions = service
            .Select(metrics => (
              metrics.Labels,
              metrics.Item2!
                .Select(versionInfo => versionInfo)
                .OrderByDescending(v => v.Timestamp)
                .FirstOrDefault()))
            .GroupBy(
              val => val.Item2.Timestamp,
              val => val.Labels);

          foreach (var timestampGroup in timestampedVersions) {
            var timestamp = this._versionDataHelper
              .ConvertDecimalTimestampToDateTime(timestampGroup.Key);
            var versionTypeInfo = ToServiceVersionTypeInfo(timestampGroup);
            versionsByTimestamp.Add((timestamp, versionTypeInfo.ToImmutableList()));
          }

          serviceVersionHistoryMap.Add(serviceName, versionsByTimestamp.ToImmutableList());
        }

        return serviceVersionHistoryMap;
      },
      cancellationToken);
  }

  private async Task<IImmutableList<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>>
    GetServiceVersionsHistory(
      String environment,
      String tenant,
      String service,
      String range,
      DateTime timeQuery,
      CancellationToken cancellationToken) {

    String promQuery = $"{VersionDataHelper.ServiceVersionAggregateMetricName}" +
      $"{{environment=\"{environment}\", tenant=\"{tenant}\", service=\"{service}\"}}" +
      $"[{range}]";

    return await this._prometheusQueryHelper.GetInstantaneousValuePromQuery(
      promQuery,
      timeQuery,
      processResult: results => {

        var versionsByTimestamp = new List<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>();

        var timestampedVersions = results.Result
          .Where(series => (series.Values != null) && series.Values.Any())
          .Select(series => (
            series.Labels,
            series.Values!
              .OrderByDescending(versionInfo => versionInfo.Timestamp)
              .FirstOrDefault()))
          .GroupBy(
            val => val.Item2.Timestamp,
            val => val.Labels);

        foreach (var timestampGroup in timestampedVersions) {
          var timestamp = this._versionDataHelper
            .ConvertDecimalTimestampToDateTime(timestampGroup.Key);
          var versionTypeInfo = ToServiceVersionTypeInfo(timestampGroup);
          versionsByTimestamp.Add((timestamp, versionTypeInfo.ToImmutableList()));
        }

        return versionsByTimestamp.ToImmutableList();
      },
      cancellationToken);
  }

  private static List<ServiceVersionTypeInfo> ToServiceVersionTypeInfo(
    IGrouping<Decimal, IImmutableDictionary<String, String>> timestampGroup) {

    var versionTypeInfo = new List<ServiceVersionTypeInfo>();

    foreach (var group in timestampGroup) {

      if (!group.TryGetValue(VersionDataHelper.ServiceVersionTypeLabelName,
        out var versionTypeValue)) {
        throw new InvalidOperationException(
          $"The time series for version is missing the {VersionDataHelper.ServiceVersionTypeLabelName} label");
      }

      if (!group.TryGetValue(VersionDataHelper.ServiceVersionValueLabelName,
        out var versionValue)) {
        throw new InvalidOperationException(
          $"The time series for version is missing the {VersionDataHelper.ServiceVersionValueLabelName} label");
      }

      if (Enum.TryParse<VersionCheckType>(versionTypeValue, out var versionType)) {
        versionTypeInfo.Add(new ServiceVersionTypeInfo(versionType, versionValue));
      }
    }

    return versionTypeInfo;
  }

  private static (String, DateTime) ValidateQueryParameters(
    Int32? duration,
    DateTime? timeQuery) {

    // Default duration for the query is 60 seconds
    var durationInSeconds = duration ?? 60;
    var queryDuration = PrometheusClient.ToPrometheusDuration(TimeSpan.FromSeconds(durationInSeconds));

    var queryTimestamp = timeQuery ?? DateTime.UtcNow;
    if (queryTimestamp.Kind != DateTimeKind.Utc) {
      throw new BadRequestException("Invalid timestamp. Time must be expressed in UTC.");
    }

    return (queryDuration, queryTimestamp);
  }
}
