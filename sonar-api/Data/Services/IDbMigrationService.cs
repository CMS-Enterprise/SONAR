using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Data.Services;

public interface IDbMigrationService {

  /// <summary>
  /// Apply all pending migrations to the existing database.
  /// </summary>
  /// <remarks>
  /// Requires an exclusive access lock on the migrations history table to ensure only one process in the production
  /// environment can apply changes to the database; therefore, the database and the migrations history table must
  /// already exist when this is called.
  /// </remarks>
  /// <returns>True if the database was migrated, false if it was not.</returns>
  Task<Boolean> MigrateDbAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Migrate the database to the given target migration. If the migration is pending then the database is upgraded
  /// (possibly applying any intermediate pending migrations), and if the migration has already been applied the
  /// database is downgraded (possibly rolling back any intermediate migrations to get there).
  /// </summary>
  /// <remarks>
  /// This can be a potentially destructive operation if targeting an already applied migration! This method takes
  /// no measures to back up or otherwise preserve data in the case of a downgrade where tables or columns may be
  /// dropped along the way to the target migration.
  /// </remarks>
  /// <param name="targetMigration">The ID of the target migration to migrate the database to.</param>
  /// <param name="cancellationToken">A cancellation token for the async operation.</param>
  /// <returns>True if the database was migrated, false if it was not.</returns>
  Task<Boolean> MigrateDbAsync(String targetMigration, CancellationToken cancellationToken = default);

  /// <summary>
  /// Drop the existing database if it exists, then re-create and apply the CreateDb migration.
  /// </summary>
  /// <remarks>
  /// This is only intended for use in development environments!
  /// </remarks>
  /// <returns>True if the database was deleted, false if it was not.</returns>
  Task<Boolean> ReCreateDbAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Ensure the EntityFramework migrations history table exists in the database.
  /// </summary>
  /// This method executes raw transactional DDL to create the EntityFramework migrations history table prior to
  /// entering any EF migration code. Because the Sonar API database migration strategy requires an exclusive lock
  /// on the migrations history table prior to running EF migrations, this provides a safe way to ensure that table
  /// exists in fresh DBs that haven't been migrated yet.
  /// <returns>True if the table was created, false if it already exists.</returns>
  Task<Boolean> ProvisionMigrationsHistoryTable(CancellationToken cancellationToken = default);
}
