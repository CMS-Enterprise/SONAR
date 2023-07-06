using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Data.Services;

public interface IDbMigrationService {

  /// <summary>
  /// Apply pending migrations to the existing database.
  /// </summary>
  /// <remarks>
  /// Requires an exclusive access lock on the migrations history table to ensure only one process in the production
  /// environment can apply changes to the database; therefore, the database and the migrations history table must
  /// already exist when this is called.
  /// </remarks>
  /// <returns>True if the database was migrated, false if it was not.</returns>
  Task<Boolean> MigrateDbAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Drop the existing database if it exists, then re-create and apply the CreateDb migration.
  /// </summary>
  /// <remarks>
  /// This is only intended for use in development environments!
  /// </remarks>
  /// <returns>True if the database was deleted, false if it was not.</returns>
  Task<Boolean> ReCreateDbAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Ensure the EntityFramework migrations history table exists in the database; if the table does not exist,
  /// create it and insert the CreateDb and InitialMigration records.
  /// </summary>
  /// <remarks>
  /// This is only for the initial conversion from non-migration support to migration support, and is very
  /// specific to the state of the Sonar API implementation at the time when we are going to do this conversion!
  /// This code will be removed after we have converted all of our existing deployments of Sonar API.
  /// </remarks>
  /// <returns></returns>
  Task<Boolean> ProvisionMigrationsHistoryTable(CancellationToken cancellationToken = default);
}
