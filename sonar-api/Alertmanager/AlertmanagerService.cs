using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Alertmanager;

public class AlertmanagerService : IAlertmanagerService {

  private readonly ILogger<AlertmanagerService> _logger;
  private readonly IAlertmanagerClient _alertmanager;

  public AlertmanagerService(
    ILogger<AlertmanagerService> logger,
    IAlertmanagerClient alertmanager) {
    this._logger = logger;
    this._alertmanager = alertmanager;
  }

  /// <inheritdoc />
  public async Task<IImmutableList<GettableAlert>> GetActiveAlertsAsync(
    String environmentName,
    String tenantName,
    String serviceName,
    CancellationToken cancellationToken) {

    var activeAlerts = await this._alertmanager.GetAlertsAsync(
      active: true,
      silenced: true,
      inhibited: false,
      unprocessed: true,
      filter: new[] {
        $"{IAlertmanagerService.EnvironmentLabel}={environmentName}",
        $"{IAlertmanagerService.TenantLabel}={tenantName}",
        $"{IAlertmanagerService.ServiceLabel}={serviceName}"
      },
      receiver: ".*",
      cancellationToken) as IEnumerable<GettableAlert>;
    if (activeAlerts == null) {
      return ImmutableList<GettableAlert>.Empty;
    }
    return activeAlerts.ToImmutableList();
  }

  /// <inheritdoc />
  public async Task<HealthStatus> GetAlwaysFiringAlertStatusAsync(CancellationToken cancellationToken) {
    var status = HealthStatus.Online;

    try {
      var alertsQueryResult = await this._alertmanager.GetAlertsAsync(
        active: true,
        silenced: true,
        inhibited: true,
        unprocessed: true,
        filter: new[] {
          $"{IAlertmanagerService.AlertNameLabel}={IAlertmanagerService.AlwaysFiringAlertName}"
        },
        receiver: ".*",
        cancellationToken);

      var alwaysFiringAlert = alertsQueryResult.SingleOrDefault();

      if (alwaysFiringAlert is not null) {
        var lastUpdateAge = DateTime.UtcNow.Subtract(alwaysFiringAlert.UpdatedAt.UtcDateTime);
        // We _should_ see this alert updated once for every Prometheus rule evaluation interval, but allow a little
        // slop for things like network delays.
        var maxAgeThreshold = IPrometheusService.RuleEvaluationInterval + TimeSpan.FromMinutes(1);

        if (lastUpdateAge > maxAgeThreshold) {
          this._logger.LogError(message: "The always firing alert is older than {threshold}.", maxAgeThreshold);
          status = HealthStatus.Degraded;
        }
      } else {
        this._logger.LogError("The always firing alert is not currently firing.");
        status = HealthStatus.Offline;
      }
    } catch (Exception e) {
      this._logger.LogError(e, message: "Unexpected error while retrieving the always firing alert.");
      status = HealthStatus.Unknown;
    }

    return status;
  }

  public async Task CreateUpdateSilenceAsync(
    String environmentName,
    String tenantName,
    String serviceName,
    String alertName,
    String userName,
    CancellationToken cancellationToken) {

    var activeSilences = await this.GetActiveSilences(
      environmentName,
      tenantName,
      serviceName,
      alertName,
      cancellationToken);
    var existingActiveSilence = activeSilences.MaxBy(s => s.UpdatedAt);

    // create silence, duration will be 1 day from current time.
    var start = DateTime.UtcNow;
    var silenceObj = new PostableSilence() {
      StartsAt = new DateTimeOffset(start),
      EndsAt = new DateTimeOffset(start.AddDays(1)),
      Matchers = GetMatchers(environmentName, tenantName, serviceName, alertName),
      Comment = IAlertmanagerService.SilenceComment,
      CreatedBy = userName
    };

    if (existingActiveSilence != null) {
      silenceObj.Id = existingActiveSilence.Id;
    }

    await this._alertmanager.PostSilencesAsync(silenceObj, cancellationToken);
  }

  public async Task DeleteSilenceAsync(
    String environmentName,
    String tenantName,
    String serviceName,
    String alertName,
    CancellationToken cancellationToken) {

    var existingActiveSilences = await this.GetActiveSilences(
      environmentName,
      tenantName,
      serviceName,
      alertName,
      cancellationToken);

    // delete each active silence for a given alert
    var deleteSilencesTaskList = existingActiveSilences.Select(silence =>
      this._alertmanager.DeleteSilenceAsync(new Guid(silence.Id), cancellationToken));

    await Task.WhenAll(deleteSilencesTaskList);
  }

  private async Task<ImmutableList<GettableSilence>> GetActiveSilences(
    String environment,
    String tenant,
    String service,
    String alertName,
    CancellationToken cancellationToken) {

    var silences = await this._alertmanager.GetSilencesAsync(
      filter: new[] {
        $"{IAlertmanagerService.EnvironmentLabel}={environment}",
        $"{IAlertmanagerService.TenantLabel}={tenant}",
        $"{IAlertmanagerService.ServiceLabel}={service}",
        $"{IAlertmanagerService.AlertNameLabel}={alertName}"
      },
      cancellationToken);

    if (silences == null) {
      return ImmutableList<GettableSilence>.Empty;
    }

    return silences
      .Where(s =>
        s.Status.State == SilenceStatusState.Active)
      .ToImmutableList();
  }

  public async Task<ImmutableList<GettableSilence>> GetActiveServiceSilencesAsync(
    String environment,
    String tenant,
    String service,
    CancellationToken cancellationToken) {

    var serviceSilences = await this._alertmanager.GetSilencesAsync(
      filter: new[] {
        $"{IAlertmanagerService.EnvironmentLabel}={environment}",
        $"{IAlertmanagerService.TenantLabel}={tenant}",
        $"{IAlertmanagerService.ServiceLabel}={service}"
      },
      cancellationToken);

    if (serviceSilences == null) {
      return ImmutableList<GettableSilence>.Empty;
    }

    return serviceSilences
      .Where(s =>
        s.Status.State == SilenceStatusState.Active)
      .ToImmutableList();
  }

  private Matchers GetMatchers(
    String environmentName,
    String tenantName,
    String serviceName,
    String alertName) {

    return new Matchers() {
      new Matcher() { Name = IAlertmanagerService.AlertNameLabel, Value = alertName, IsRegex = false },
      new Matcher() { Name = IAlertmanagerService.EnvironmentLabel, Value = environmentName, IsRegex = false },
      new Matcher() { Name = IAlertmanagerService.TenantLabel, Value = tenantName, IsRegex = false },
      new Matcher() { Name = IAlertmanagerService.ServiceLabel, Value = serviceName, IsRegex = false }
    };
  }
}
