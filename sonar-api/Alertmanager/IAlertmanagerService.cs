using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Alertmanager;

public interface IAlertmanagerService {

  // All alerts should have this label as part of the Alertmanager API contract.
  public const String AlertNameLabel = "alertname";
  public const String EnvironmentLabel = "environment";
  public const String TenantLabel = "tenant";
  public const String ServiceLabel = "service";
  public const String SilenceComment = "SONAR Alert silenced by user.";
  public const String AlwaysFiringAlertName = "always-firing";

  /// <summary>
  /// Get the (possibly empty) list of active SONAR service alerts matching the given
  /// environment, tenant, and service names.
  /// </summary>
  Task<IImmutableList<GettableAlert>> GetActiveAlertsAsync(
    String environmentName,
    String tenantName,
    String serviceName,
    CancellationToken cancellationToken);

  /// <summary>
  /// Gets the current status of the always-firing alert. This is a health check to ensure that firing alerts are
  /// getting reported by Prometheus to Alertmanager. Prometheus is expected to regularly fire this special alert.
  /// </summary>
  /// <returns>
  /// <see cref="HealthStatus.Online"/> if the always-firing alert is present and it has been updated recently.
  /// <see cref="HealthStatus.Degraded"/> if the always-firing alert is present but hasn't been updated recently.
  /// <see cref="HealthStatus.Offline"/> if the always firing alert is missing.
  /// <see cref="HealthStatus.Unknown"/> if there was any unexpected error querying for the always-firing
  /// alert (such as an unsuccessful HTTP response, or an unexpected query result).
  /// </returns>
  Task<HealthStatus> GetAlwaysFiringAlertStatusAsync(CancellationToken cancellationToken);

  Task CreateUpdateSilenceAsync(
    String environmentName,
    String tenantName,
    String serviceName,
    String alertName,
    String userName,
    CancellationToken cancellationToken);

  Task DeleteSilenceAsync(
    String environmentName,
    String tenantName,
    String serviceName,
    String alertName,
    CancellationToken cancellationToken);

  Task<ImmutableList<GettableSilence>> GetActiveServiceSilencesAsync(
    String environment,
    String tenant,
    String service,
    CancellationToken cancellationToken);
}
