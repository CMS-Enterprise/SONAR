using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cms.BatCave.Sonar.Helpers.Maintenance;

public class MaintenanceDataHelper<TMaintenance> where TMaintenance : class, Data.Maintenance {

  private DataContext Context { get; init; }
  private DbSet<TMaintenance> MaintenanceTable { get; init; }
  private Func<Guid, Expression<Func<TMaintenance, Boolean>>> GetAssocEntityIdFilter { get; init; }

  public MaintenanceDataHelper(
    DataContext context,
    DbSet<TMaintenance> maintenanceTable,
    Func<Guid, Expression<Func<TMaintenance, Boolean>>> getAssocEntityIdFilter) {
    this.Context = context;
    this.MaintenanceTable = maintenanceTable;
    this.GetAssocEntityIdFilter = getAssocEntityIdFilter;
  }

  public async Task<IImmutableList<TMaintenance>> FindAllByAssocEntityIdAsync(Guid id, CancellationToken ct) =>
    (await this.MaintenanceTable.Where(this.GetAssocEntityIdFilter.Invoke(id)).ToListAsync(ct)).ToImmutableList();

  public async Task<IImmutableList<TMaintenance>> FindAllByAssocEntityIdsAsync(IEnumerable<Guid> ids, CancellationToken ct) {
    static Expression<T> Or<T>(Expression<T> e1, Expression<T> e2) =>
      Expression.Lambda<T>(Expression.OrElse(e1.Body, Expression.Invoke(e2, e1.Parameters)), e1.Parameters);

    var predicate = ids.Aggregate<Guid, Expression<Func<TMaintenance, Boolean>>>(
      seed: _ => false,
      func: (predicate, id) => Or(predicate, this.GetAssocEntityIdFilter.Invoke(id)));

    return (await this.MaintenanceTable.Where(predicate).ToListAsync(ct)).ToImmutableList();
  }

  public async Task<TMaintenance?> SingleOrDefaultByAssocEntityIdAsync(Guid id, CancellationToken ct) =>
    await this.MaintenanceTable.SingleOrDefaultAsync(this.GetAssocEntityIdFilter.Invoke(id), ct);

  public async Task<Int32> ExecuteDeleteByAssocEntityIdAsync(Guid id, CancellationToken ct) =>
    await this.MaintenanceTable.Where(this.GetAssocEntityIdFilter.Invoke(id)).ExecuteDeleteAsync(ct);

  public async Task RemoveRangeAsync(TMaintenance[] maintenances, CancellationToken ct) {
    this.MaintenanceTable.RemoveRange(maintenances);
    await this.Context.SaveChangesAsync(ct);
  }

  public async Task<TMaintenance> AddAsync(TMaintenance entity, CancellationToken ct) {
    var entityEntry = await this.MaintenanceTable.AddAsync(entity, ct);
    await this.Context.SaveChangesAsync(ct);
    return entityEntry.Entity;
  }

  public async Task<IImmutableList<TMaintenance>> AddAllAsync(IEnumerable<TMaintenance> entities, CancellationToken ct) {
    var createdEntities = await this.MaintenanceTable.AddAllAsync(entities, ct);
    await this.Context.SaveChangesAsync(ct);
    return createdEntities;
  }

  public async Task<IImmutableList<TMaintenance>> FindAllAsync(CancellationToken ct) =>
    (await this.MaintenanceTable.ToListAsync(ct)).ToImmutableList();

  /// <summary>
  /// Marks the maintenance configurations of the current <typeparamref name="TMaintenance"/> type as locked for
  /// recording. Only one concurrent process can lock the candidate rows, all others will fail to commit the locking
  /// transaction. This is to ensure that only one pod (in environments where multiple sonar-api pods are deployed)
  /// handles recording of maintenance status per recording period.
  /// </summary>
  /// <remarks>
  /// Status for any given maintenance configuration should only be recorded once (by one pod) per recording period,
  /// so locks can only be acquired on records where the previous lock was at least one period ago. In the event that
  /// a process acquired a lock, but failed catastrophically during recording and did not release the lock, another
  /// process may acquire a new lock on that record if the last lock is over two recording periods ago.
  /// </remarks>
  /// <param name="lastRecorded">The timestamp to set for the lock.</param>
  /// <param name="recordingPeriod">The recording period for determining which records are lockable.</param>
  /// <param name="ct">A cancellation token for the async operation.</param>
  /// <returns>
  /// If the lock is successful, a list of the locked maintenance configurations is returned. If the lock
  /// fails, an empty list is returned.
  /// </returns>
  public async Task<IImmutableList<TMaintenance>> LockForRecordingAsync(
    DateTime lastRecorded,
    TimeSpan recordingPeriod,
    CancellationToken ct) {

    var t1 = lastRecorded - recordingPeriod;
    var t2 = lastRecorded - (2 * recordingPeriod);
    Expression<Func<TMaintenance, Boolean>> candidateRowLockPredicate = row =>
      (!row.IsRecording && ((row.LastRecorded == null) || (row.LastRecorded < t1))) || (row.LastRecorded < t2);

    var tx = await this.Context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
    var numLocked = 0;

    try {
      numLocked = await this.MaintenanceTable
        .Where(candidateRowLockPredicate)
        .ExecuteUpdateAsync(
          setPropertyCalls: row =>
            row.SetProperty(r => r.IsRecording, true)
              .SetProperty(r => r.LastRecorded, lastRecorded),
          ct);

      await tx.CommitAsync(ct);
    } catch (Exception e) when (e.InnerException is PostgresException {
      SqlState: PostgresErrorCodes.SerializationFailure
    }) {
      // If we got the 'could not serialize access due to concurrent update' error, that means
      // another process got the lock, and we should quietly abort. Any other exception we throw.
      await tx.RollbackAsync(ct);
    } catch (Exception) {
      await tx.RollbackAsync(ct);
      throw;
    }

    var lockedRows = ImmutableList<TMaintenance>.Empty;

    if (numLocked > 0) {
      lockedRows = (await this.MaintenanceTable
          .Where(row => row.IsRecording && (row.LastRecorded == lastRecorded))
          .ToListAsync(ct))
        .ToImmutableList();
    }

    return lockedRows;
  }

  /// <summary>
  /// Releases the recording lock for the given list of maintenance configurations. Called after the process that
  /// locked and recorded maintenance status for the configurations has finished recording.
  /// </summary>
  public async Task ReleaseRecordingLockAsync(IImmutableList<TMaintenance> maintenances, CancellationToken ct) {
    await this.MaintenanceTable
      .Where(row => maintenances.Select(m => m.Id).Contains(row.Id))
      .ExecuteUpdateAsync(setPropertyCalls: row => row.SetProperty(r => r.IsRecording, false), ct);
  }
}

public static class MdhServicesExtensions {
  public static IServiceCollection AddMaintenanceDataHelpers(this IServiceCollection services) {
    return services
      .AddSingleton<Func<Guid, Expression<Func<ScheduledEnvironmentMaintenance, Boolean>>>>(id =>
        m => m.EnvironmentId == id)
      .AddSingleton<Func<Guid, Expression<Func<ScheduledTenantMaintenance, Boolean>>>>(id =>
        m => m.TenantId == id)
      .AddSingleton<Func<Guid, Expression<Func<ScheduledServiceMaintenance, Boolean>>>>(id =>
        m => m.ServiceId == id)
      .AddSingleton<Func<Guid, Expression<Func<AdHocEnvironmentMaintenance, Boolean>>>>(id =>
        m => m.EnvironmentId == id)
      .AddSingleton<Func<Guid, Expression<Func<AdHocTenantMaintenance, Boolean>>>>(id =>
        m => m.TenantId == id)
      .AddSingleton<Func<Guid, Expression<Func<AdHocServiceMaintenance, Boolean>>>>(id =>
        m => m.ServiceId == id)
      .AddScoped<MaintenanceDataHelper<ScheduledEnvironmentMaintenance>>()
      .AddScoped<MaintenanceDataHelper<ScheduledTenantMaintenance>>()
      .AddScoped<MaintenanceDataHelper<ScheduledServiceMaintenance>>()
      .AddScoped<MaintenanceDataHelper<AdHocEnvironmentMaintenance>>()
      .AddScoped<MaintenanceDataHelper<AdHocTenantMaintenance>>()
      .AddScoped<MaintenanceDataHelper<AdHocServiceMaintenance>>();
  }
}
