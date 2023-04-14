using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models.Legacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/services")]
public class LegacyController : ControllerBase {
  private readonly HealthDataHelper _healthDataHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly IOptions<LegacyEndpointConfiguration> _configuration;
  private readonly ILogger<LegacyController> _logger;

  public LegacyController(
    HealthDataHelper healthDataHelper,
    ServiceDataHelper serviceDataHelper,
    IOptions<LegacyEndpointConfiguration> configuration,
    ILogger<LegacyController> logger) {

    this._healthDataHelper = healthDataHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._configuration = configuration;
    this._logger = logger;
  }

  [HttpGet]
  [HttpGet("/")]
  public async Task<IActionResult> GetAllServices(CancellationToken cancellationToken) {
    var config = this._configuration.Value;

    if (!config.Enabled) {
      return this.NotFound();
    }

    var (serviceMapping, rootServices) = ValidateConfiguration(config);

    var tenants =
      serviceMapping
        .Where(svc => svc.Environment != null)
        .Select(svc => (svc.Environment!, svc.Tenant!))
        .ToImmutableHashSet(CaseInsensitiveTupleComparer.Instance);

    var tenantServices =
      new Dictionary<(String, String), IDictionary<String, (Service Config, (DateTime, HealthStatus)? Status)>>(
        CaseInsensitiveTupleComparer.Instance
      );

    // Fetch the service configs and statuses for any tenants referenced by the legacy configuration
    foreach (var (environment, tenant) in tenants) {
      var serviceConfigs = await this.GetServiceConfigurationsByName(environment, tenant, cancellationToken);
      var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(environment, tenant, cancellationToken);

      tenantServices.Add(
        (environment, tenant),
        LeftOuterMerge(
          serviceConfigs,
          serviceStatuses,
          (svc, status) => (svc, status),
          StringComparer.OrdinalIgnoreCase
        )
      );
    }

    var serviceLookup =
      serviceMapping
        .Select(svc => {
          if ((svc.Environment != null) && (svc.Tenant != null)) {
            if (tenantServices.TryGetValue((svc.Environment, svc.Tenant), out var tenantStatuses)) {
              if (tenantStatuses.TryGetValue(svc.Name!, out var serviceInfo)) {
                return new LegacyServiceInfo(svc, serviceInfo.Config, serviceInfo.Status);
              } else {
                this._logger.LogWarning(
                  "The v1 service endpoint configuration referenced a service that was not found (Environment: {Environment}, Tenant: {Tenant}): {ServiceName}",
                  svc.Environment,
                  svc.Tenant,
                  svc.Name);
                return new LegacyServiceInfo(svc);
              }
            } else {
              this._logger.LogWarning(
                "The v1 service endpoint configuration referenced a tenant that was not found (Environment: {Environment}): {Tenant}",
                svc.Environment,
                svc.Tenant
              );
              return new LegacyServiceInfo(svc);
            }
          } else {
            return new LegacyServiceInfo(svc);
          }
        })
        .ToDictionary(
          svc => svc.LegacyService.LegacyName,
          StringComparer.OrdinalIgnoreCase);

    return this.Ok(rootServices.Select(svc => ToServiceHierarchy(serviceLookup, svc)).ToArray());
  }

  private async Task<IDictionary<String, Service>> GetServiceConfigurationsByName(
    String environment,
    String tenant,
    CancellationToken cancellationToken) {

    var (_, _, servicesById) =
      await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);

    return servicesById.Select(kvp => kvp.Value).ToDictionary(svc => svc.Name, StringComparer.OrdinalIgnoreCase);
  }

  private static LegacyServiceHierarchyHealth ToServiceHierarchy(
    IDictionary<String, LegacyServiceInfo> serviceLookup,
    String service) {

    var svc = serviceLookup[service];

    var children =
      svc.LegacyService.GetChildren()
        .Select(child => ToServiceHierarchy(serviceLookup, child))
        .ToImmutableList();

    var aggregateStatus =
      svc.ServiceStatus.HasValue ? ToLegacyStatus(svc.ServiceStatus.Value.Item2) : (LegacyHealthStatus?)null;

    foreach (var child in children) {
      if (aggregateStatus == null) {
        aggregateStatus = child.Status;
      } else if (aggregateStatus < child.Status) {
        aggregateStatus = child.Status;
      }
    }

    return new LegacyServiceHierarchyHealth(
      svc.LegacyService.LegacyName,
      svc.LegacyService.DisplayName ?? svc.MappedConfig?.DisplayName,
      svc.MappedConfig?.Description,
      aggregateStatus ?? LegacyHealthStatus.Unresponsive,
      svc.MappedConfig?.Url?.ToString(),
      children
    );
  }

  private static LegacyHealthStatus ToLegacyStatus(HealthStatus status) {
    switch (status) {
      case HealthStatus.Unknown:
      case HealthStatus.Offline:
        return LegacyHealthStatus.Unresponsive;
      case HealthStatus.Online:
        return LegacyHealthStatus.Operational;
      case HealthStatus.AtRisk:
      case HealthStatus.Degraded:
        return LegacyHealthStatus.Degraded;
      default:
        throw new ArgumentOutOfRangeException(nameof(status), status, $"Unknown value for {nameof(HealthStatus)}.");
    }
  }

  private static (LegacyServiceMapping[] serviceMapping, String[] rootServices)
    ValidateConfiguration(
      LegacyEndpointConfiguration config) {

    if (config.ServiceMapping == null) {
      throw new InvalidOperationException(
        $"The v1 services endpoint configuration has a null {nameof(LegacyEndpointConfiguration.ServiceMapping)}"
      );
    }

    if (config.RootServices == null) {
      throw new InvalidOperationException(
        $"The v1 services endpoint configuration has a null {nameof(LegacyEndpointConfiguration.RootServices)}"
      );
    }

    var names = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    foreach (var svc in config.ServiceMapping) {
      if (names.Contains(svc.LegacyName)) {
        throw new InvalidOperationException(
          $"The v1 services endpoint configuration has two services with equivalent names: {svc.LegacyName}"
        );
      }

      names.Add(svc.LegacyName);
    }

    foreach (var root in config.RootServices) {
      if (!names.Contains(root)) {
        throw new InvalidOperationException(
          $"The v1 services endpoint configuration has a root service that does not exist in the service list: {root}"
        );
      }
    }

    return (config.ServiceMapping, config.RootServices);
  }

  private static IDictionary<TKey, TOutValue> LeftOuterMerge<TKey, TValue1, TValue2, TOutValue>(
    IDictionary<TKey, TValue1> leftDictionary,
    IDictionary<TKey, TValue2> rightDictionary,
    Func<TValue1, TValue2?, TOutValue> merge,
    IEqualityComparer<TKey> comparer) where TKey : notnull where TValue2 : struct {

    return leftDictionary.ToDictionary(
      kvp => kvp.Key,
      kvp => merge(kvp.Value, rightDictionary.TryGetValue(kvp.Key, out var right) ? right : null),
      comparer
    );
  }

  private class CaseInsensitiveTupleComparer : IEqualityComparer<(String, String)> {
    public static readonly CaseInsensitiveTupleComparer Instance = new CaseInsensitiveTupleComparer();

    private CaseInsensitiveTupleComparer() {
    }

    public Boolean Equals((String, String) x, (String, String) y) {
      return String.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
        String.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
    }

    public Int32 GetHashCode((String, String) obj) {
      var hashCode = new HashCode();
      hashCode.Add(obj.Item1, StringComparer.OrdinalIgnoreCase);
      hashCode.Add(obj.Item2, StringComparer.OrdinalIgnoreCase);
      return hashCode.ToHashCode();
    }
  }

  private record LegacyServiceInfo(
    LegacyServiceMapping LegacyService,
    Service? MappedConfig = null,
    (DateTime, HealthStatus)? ServiceStatus = null) {
    public override String ToString() {
      return $"{{ LegacyService = {LegacyService}, MappedConfig = {MappedConfig}, ServiceStatus = {ServiceStatus} }}";
    }
  }
}
