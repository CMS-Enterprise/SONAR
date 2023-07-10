using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
    await using var transaction = await this._dataContext.Database.BeginTransactionAsync(cancellationToken);

    this._logger.LogInformation("Awaiting lock on migrations history table to perform database migrations.");
    await this._dataContext.Database.ExecuteSqlRawAsync(
      sql: @"LOCK TABLE ONLY ""__EFMigrationsHistory"" IN ACCESS EXCLUSIVE MODE;",
      cancellationToken);

    var pendingMigrations =
      (await this._dataContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToImmutableList();

    var migrated = false;

    if (pendingMigrations.Any()) {
      this._logger.LogInformation(
        message: "Applying {count} database migration(s): {migrations}",
        pendingMigrations.Count,
        pendingMigrations);

      await this._dataContext.Database.MigrateAsync(cancellationToken);

      migrated = true;
    } else {
      this._logger.LogInformation($"The database is up to date, no pending migrations.");
    }

    // TODO BATAPI-325 Remove; adding time delay for testing purposes.
    this._logger.LogInformation("5s testing delay...");
    await Task.Delay(5 * 1000, cancellationToken);
    this._logger.LogInformation("...Done.");

    await transaction.CommitAsync(cancellationToken);

    return migrated;
  }

  /// <inheritdoc />
  public async Task<Boolean> ReCreateDbAsync(CancellationToken cancellationToken = default) {
    this._logger.LogInformation("Re-creating database!");
    var deleted = await this._dataContext.Database.EnsureDeletedAsync(cancellationToken);

    var migrator = this._dataContext.GetInfrastructure().GetRequiredService<IMigrator>();
    await migrator.MigrateAsync(targetMigration: "20230101000000_CreateDb", cancellationToken);

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

      INSERT INTO ""__EFMigrationsHistory"" VALUES ('20230101000000_CreateDb', '7.0.8');
      INSERT INTO ""__EFMigrationsHistory"" VALUES ('20230629163433_InitialMigration', '7.0.8');

      COMMIT;
    ";

    try {
      await this._dataContext.Database.ExecuteSqlRawAsync(provisionMigrationsHistoryTableSql, cancellationToken);
      this._logger.LogInformation("Migrations history table created.");
    } catch (PostgresException e) when (e is { SqlState: "42P07" }) {
      this._logger.LogInformation("Migrations history table exists.");
    }

    return true;
  }
}
