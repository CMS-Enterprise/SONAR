using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cms.BatCave.Sonar.Data.Services;

public class DbMigrationService : IDbMigrationService {

  private readonly ILogger<DbMigrationService> _logger;
  private readonly DataContext _dataContext;
  private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;

  public DbMigrationService(
    ILogger<DbMigrationService> logger,
    DataContext dataContext,
    IOptions<DatabaseConfiguration> databaseConfiguration) {
    this._logger = logger;
    this._dataContext = dataContext;
    this._databaseConfiguration = databaseConfiguration;
  }

  /// <inheritdoc />
  public async Task<Boolean> MigrateDbAsync(CancellationToken cancellationToken = default) {
    await using var transaction = await this.LockMigrationsHistoryTable(cancellationToken);

    var pendingMigrations =
      (await this._dataContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToImmutableList();

    var migrated = false;

    if (pendingMigrations.Any()) {
      this._logger.LogInformation(
        message: "Applying {count} database migration(s): {migrations}",
        pendingMigrations.Count,
        pendingMigrations);

      await this._dataContext.Database.MigrateAsync(cancellationToken);

      this._logger.LogInformation("Database migration complete.");
      migrated = true;
    } else {
      this._logger.LogInformation($"The database is up to date, no pending migrations.");
    }

    await transaction.CommitAsync(cancellationToken);

    return migrated;
  }

  /// <inheritdoc />
  public async Task<Boolean> MigrateDbAsync(String targetMigration, CancellationToken cancellationToken = default) {
    await using var transaction = await this.LockMigrationsHistoryTable(cancellationToken);

    var appliedMigrations =
      (await this._dataContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToImmutableList();

    var migrated = false;

    if (appliedMigrations.Any() && appliedMigrations.Last().Equals(targetMigration)) {
      this._logger.LogInformation(
        message: "The database is already at target migration: {targetMigration}",
        targetMigration);
    } else {
      this._logger.LogInformation(
        message: "{migrationAction} database to target migration: {targetMigration}",
        appliedMigrations.Contains(targetMigration) ? "Downgrading" : "Upgrading",
        targetMigration);

      await this.MigrateDbTo(targetMigration, cancellationToken);

      this._logger.LogInformation("Database migration complete.");
      migrated = true;
    }

    await transaction.CommitAsync(cancellationToken);

    return migrated;
  }

  /// <inheritdoc />
  public async Task<Boolean> ReCreateDbAsync(CancellationToken cancellationToken = default) {
    this._logger.LogInformation("Re-creating database!");

    var deleted = await this._dataContext.Database.EnsureDeletedAsync(cancellationToken);

    await this.MigrateDbTo(targetMigration: "20230101000000_CreateDb", cancellationToken);

    return deleted;
  }

  /// <inheritdoc />
  public async Task<Boolean> ProvisionMigrationsHistoryTable(CancellationToken cancellationToken = default) {
    var provisionMigrationsHistoryTableSql = $@"
      BEGIN;

      CREATE TABLE ""__EFMigrationsHistory"" (
        migration_id character varying(150) NOT NULL,
        product_version character varying(32) NOT NULL
      );

      ALTER TABLE ""__EFMigrationsHistory"" OWNER TO {this._databaseConfiguration.Value.Username};

      ALTER TABLE ONLY ""__EFMigrationsHistory""
        ADD CONSTRAINT pk__ef_migrations_history PRIMARY KEY (migration_id);

      COMMIT;
    ";

    Boolean created = false;

    try {
      await this._dataContext.Database.ExecuteSqlRawAsync(provisionMigrationsHistoryTableSql, cancellationToken);
      this._logger.LogInformation("Migrations history table created.");
      created = true;
    } catch (PostgresException e) when (e is { SqlState: "42P07" }) {
      this._logger.LogInformation("Migrations history table exists.");
    }

    return created;
  }

  private async Task<IDbContextTransaction> LockMigrationsHistoryTable(CancellationToken cancellationToken = default) {
    var transaction = await this._dataContext.Database.BeginTransactionAsync(cancellationToken);

    this._logger.LogInformation("Awaiting lock on migrations history table to perform database migrations.");

    await this._dataContext.Database.ExecuteSqlRawAsync(
      sql: @"LOCK TABLE ONLY ""__EFMigrationsHistory"" IN ACCESS EXCLUSIVE MODE;",
      cancellationToken);

    this._logger.LogInformation("Migrations history table lock acquired.");

    return transaction;
  }

  private async Task MigrateDbTo(String targetMigration, CancellationToken cancellationToken) {
    var migrator = this._dataContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
    await migrator.MigrateAsync(targetMigration, cancellationToken);
  }
}
