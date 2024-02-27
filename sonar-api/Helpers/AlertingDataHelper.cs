using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Helpers;

public class AlertingDataHelper {

  private readonly DbSet<AlertReceiver> _alertReceiversTable;
  private readonly DbSet<AlertingRule> _alertingRulesTable;
  private readonly DbSet<AlertingConfigurationVersion> _alertingConfigurationVersionTable;

  public AlertingDataHelper(
    DbSet<AlertReceiver> alertReceiversTable,
    DbSet<AlertingRule> alertingRulesTable,
    DbSet<AlertingConfigurationVersion> alertingConfigurationVersionTable) {

    this._alertReceiversTable = alertReceiversTable;
    this._alertingRulesTable = alertingRulesTable;
    this._alertingConfigurationVersionTable = alertingConfigurationVersionTable;
  }

  /// <summary>
  /// Get the (possibly empty) list of alert receivers for the given tenant.
  /// </summary>
  /// <param name="tenantId">The tenant Id to retrieve alert receivers for.</param>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>The list of alert receivers for the given tenant.</returns>
  public async Task<IImmutableList<AlertReceiver>> FetchAlertReceiversAsync(
    Guid tenantId,
    CancellationToken cancellationToken) {
    return (await this._alertReceiversTable
        .Where(r => r.TenantId == tenantId)
        .ToListAsync(cancellationToken))
      .ToImmutableList();
  }

  /// <summary>
  /// Given a tenant Id and a list of alert receiver configurations, update the alert receivers in the database
  /// for the given tenant to match the receivers in the given list; this includes adding receivers present in the
  /// list but not the DB, updating receivers in the DB that don't match the list, and removing receivers from the DB
  /// that are not in the list.
  /// </summary>
  /// <param name="tenantId">The tenant Id to update the receivers for.</param>
  /// <param name="alertReceiverConfigurations">The alert receiver configurations to update the DB with.</param>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>
  /// A flag indicating whether any data modifications will occur with this update,
  /// and the list of receiver entities for the given tenant after the update.
  /// </returns>
  public async Task<(Boolean changed, IImmutableList<AlertReceiver>)> CreateOrUpdateAlertReceiversAsync(
    Guid tenantId,
    IImmutableList<AlertReceiverConfiguration> alertReceiverConfigurations,
    CancellationToken cancellationToken) {

    var existingAlertReceiversByName = await this._alertReceiversTable
      .Where(r => r.TenantId == tenantId)
      .ToDictionaryAsync(
        keySelector: r => r.Name,
        comparer: StringComparer.OrdinalIgnoreCase,
        cancellationToken);

    // Incoming alert receiver configurations we don't have in the DB get added to DB
    var alertReceiversAdded = await this._alertReceiversTable.AddAllAsync(
      alertReceiverConfigurations
        .Where(r => !existingAlertReceiversByName.ContainsKey(r.Name))
        .Select(r => AlertReceiver.New(tenantId, r)),
      cancellationToken);

    // Incoming alert receiver configurations we do have in the DB are either updated or unchanged
    var alertReceiversUpdated = new List<AlertReceiver>();
    var alertReceiversUnchanged = new List<AlertReceiver>();

    foreach (var r in alertReceiverConfigurations.Where(r => existingAlertReceiversByName.ContainsKey(r.Name))) {
      var existing = existingAlertReceiversByName[r.Name];
      var rOptions = AlertReceiver.SerializeOptions(r.Options);

      var updateRequired =
        !existing.Type.Equals(r.Type) ||
        !existing.Options.Equals(rOptions);

      if (updateRequired) {
        alertReceiversUpdated.Add(
          new AlertReceiver(
            id: existing.Id,
            tenantId: existing.TenantId,
            name: existing.Name,
            type: r.Type,
            options: rOptions));
      } else {
        alertReceiversUnchanged.Add(existing);
      }
    }

    this._alertReceiversTable.UpdateRange(alertReceiversUpdated);

    // Alert receiver configurations we have in the DB but not in the incoming list get deleted from DB
    var alertReceiversDeleted = existingAlertReceiversByName.Values
      .ExceptBy(
        alertReceiverConfigurations.Select(r => r.Name),
        keySelector: ar => ar.Name,
        StringComparer.OrdinalIgnoreCase)
      .ToImmutableList();

    this._alertReceiversTable.RemoveRange(alertReceiversDeleted);

    // Return an updated snapshot of the DB state
    var changed = alertReceiversAdded.Any() || alertReceiversUpdated.Any() || alertReceiversDeleted.Any();
    var currentReceivers = alertReceiversAdded
      .Concat(alertReceiversUpdated)
      .Concat(alertReceiversUnchanged)
      .ToImmutableList();
    return (changed, currentReceivers);
  }

  /// <summary>
  /// Delete all alert receivers from the DB for the given tenant.
  /// </summary>
  /// <param name="tenantId">The tenant Id to delete the alert receivers for.</param>
  /// <returns>Whether any rows are deleted from the database.</returns>
  public async Task<Boolean> DeleteAlertReceiversAsync(Guid tenantId) {
    var alertReceiversDeleted = await this._alertReceiversTable
      .Where(ar => ar.TenantId == tenantId)
      .ToListAsync();
    this._alertReceiversTable.RemoveRange(alertReceiversDeleted);
    return alertReceiversDeleted.Any();
  }

  /// <summary>
  /// Get the (possibly empty) list of alerting rules for the given services.
  /// </summary>
  /// <param name="serviceIds">The service Ids to retrieve alerting rules for.</param>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>The list of alerting rules for the given service Ids.</returns>
  public async Task<IImmutableList<AlertingRule>> FetchAlertingRulesAsync(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {
    return (await this._alertingRulesTable
        .Where(r => serviceIds.Contains(r.ServiceId))
        .ToListAsync(cancellationToken))
      .ToImmutableList();
  }

  /// <summary>
  /// Get the (possibly empty) list of alerting rules for the given service.
  /// </summary>
  /// <param name="serviceId">The service Id to retrieve alerting rules for.</param>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>The list of alerting rules for the given service Id.</returns>
  public async Task<IImmutableList<AlertingRule>> FetchAlertingRulesAsync(
    Guid serviceId,
    CancellationToken cancellationToken) {
    return await this.FetchAlertingRulesAsync(new[] { serviceId }, cancellationToken);
  }

  /// <summary>
  /// Given a service Id and a list of alerting rule configurations, update the alerting rules in the database
  /// for the given service to match the rules in the given list; this includes adding rules present in the
  /// list but not the DB, updating rules in the DB that don't match the list, and removing rules from the DB
  /// that are not in the list.
  /// </summary>
  /// <param name="serviceId">The service Id to update the alerting rules for.</param>
  /// <param name="alertReceivers">The alert receivers for the tenant that the service belongs to.</param>
  /// <param name="alertingRuleConfigurations">The alerting rule configurations to update the DB with.</param>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>
  /// A flag indicating whether any data modifications will occur with this update,
  /// and the list of rules entities for the given service after the update.
  /// </returns>
  public async Task<(Boolean changed, IImmutableList<AlertingRule>)> CreateOrUpdateAlertingRulesAsync(
    Guid serviceId,
    IImmutableList<AlertReceiver> alertReceivers,
    IImmutableList<AlertingRuleConfiguration> alertingRuleConfigurations,
    CancellationToken cancellationToken) {

    var alertReceiversByName = alertReceivers.ToDictionary(
      keySelector: r => r.Name,
      comparer: StringComparer.OrdinalIgnoreCase);

    var existingAlertingRulesByName = await this._alertingRulesTable
      .Where(ar => ar.ServiceId == serviceId)
      .ToDictionaryAsync(
        keySelector: ar => ar.Name,
        comparer: StringComparer.OrdinalIgnoreCase,
        cancellationToken);

    // Incoming alerting rule configurations we don't have in the DB get added to DB
    var alertingRulesAdded = await this._alertingRulesTable.AddAllAsync(
      alertingRuleConfigurations
        .Where(r => !existingAlertingRulesByName.ContainsKey(r.Name))
        .Select(r => AlertingRule.New(serviceId, alertReceiversByName[r.ReceiverName].Id, r)),
      cancellationToken);

    // Incoming alerting rule configurations we do have in the DB are either updated or unchanged
    var alertingRulesUpdated = new List<AlertingRule>();
    var alertingRulesUnchanged = new List<AlertingRule>();

    foreach (var r in alertingRuleConfigurations.Where(r => existingAlertingRulesByName.ContainsKey(r.Name))) {
      var existing = existingAlertingRulesByName[r.Name];
      var rReceiverId = alertReceiversByName[r.ReceiverName].Id;

      var updateRequired =
        !existing.AlertReceiverId.Equals(rReceiverId) ||
        !existing.Threshold.Equals(r.Threshold) ||
        !existing.Delay.Equals(r.Delay);

      if (updateRequired) {
        alertingRulesUpdated.Add(
          new AlertingRule(
            id: existing.Id,
            serviceId: existing.ServiceId,
            alertReceiverId: rReceiverId,
            name: existing.Name,
            threshold: r.Threshold,
            delay: r.Delay));
      } else {
        alertingRulesUnchanged.Add(existing);
      }
    }

    this._alertingRulesTable.UpdateRange(alertingRulesUpdated);

    // Alerting rule configurations we have in the DB but not in the incoming list get deleted from DB
    var alertingRulesDeleted = existingAlertingRulesByName.Values
      .ExceptBy(
        alertingRuleConfigurations.Select(r => r.Name),
        keySelector: r => r.Name,
        StringComparer.OrdinalIgnoreCase)
      .ToImmutableList();

    this._alertingRulesTable.RemoveRange(alertingRulesDeleted);

    // Return an updated snapshot of the DB state
    var changed = alertingRulesAdded.Any() || alertingRulesUpdated.Any() || alertingRulesDeleted.Any();
    var currentRules = alertingRulesAdded
      .Concat(alertingRulesUpdated)
      .Concat(alertingRulesUnchanged)
      .ToImmutableList();
    return (changed, currentRules);
  }

  /// <summary>
  /// Delete all alerting rules from the DB for the given service.
  /// </summary>
  /// <param name="serviceId">The service Id to delete the alerting rules for.</param>
  /// <returns>Whether any rows are deleted from the database.</returns>
  public async Task<Boolean> DeleteAlertingRulesAsync(Guid serviceId) {
    var alertingRulesToDelete = await this._alertingRulesTable
      .Where(r => r.ServiceId == serviceId)
      .ToListAsync();
    this._alertingRulesTable.RemoveRange(alertingRulesToDelete);
    return alertingRulesToDelete.Any();
  }

  /// <summary>
  /// Get the alerting configuration version.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>An AlertingConfigurationVersion object.</returns>
  public async Task<AlertingConfigurationVersion> FetchLatestAlertingConfigVersionAsync(
    CancellationToken cancellationToken) {
    var latestAlertingConfigVersion = await this._alertingConfigurationVersionTable
      .OrderByDescending(c => c.VersionNumber)
      .FirstOrDefaultAsync(cancellationToken);

    // DB is seeded with an initial row during the migration that adds the alerting config version table
    return latestAlertingConfigVersion!;
  }

  /// <summary>
  /// Get the (possibly nonexistent) alerting configuration version number.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for the async operation.</param>
  /// <returns>An Int32 for the alerting configuration version number.</returns>
  public async Task<Int32> FetchLatestAlertingConfigVersionNumberAsync(
    CancellationToken cancellationToken
  ) {
    var latestAlertingConfigVersion = await this._alertingConfigurationVersionTable
      .OrderByDescending(c => c.VersionNumber)
      .FirstOrDefaultAsync(cancellationToken);

    // DB is seeded with an initial row during the migration that adds the alerting config version table
    return latestAlertingConfigVersion!.VersionNumber;
  }
}
