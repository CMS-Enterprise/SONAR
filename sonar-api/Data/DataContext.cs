using System;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cms.BatCave.Sonar.Data;

public class DataContext : DbContext {
  public const String CaseInsensitiveCollation = "ci_collation";

  private readonly IOptions<DatabaseConfiguration> _configuration;
  private readonly ILoggerFactory _loggerFactory;

  public DataContext(IOptions<DatabaseConfiguration> configuration, ILoggerFactory loggerFactory) {
    this._configuration = configuration;
    this._loggerFactory = loggerFactory;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder options) {
    // connect to postgres with connection string from app settings
    var configInstance = this._configuration.Value;
    var connectionStringBuilder = new NpgsqlConnectionStringBuilder {
      Host = configInstance.Host,
      Port = configInstance.Port,
      Username = configInstance.Username,
      Password = configInstance.Password,
      Database = configInstance.Database
    };

    options
      .UseNpgsql(connectionStringBuilder.ToString())
      .UseSnakeCaseNamingConvention()
      .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
      .AddInterceptors(new DbMetrics());

    if (configInstance.DbLogging) {
      options.UseLoggerFactory(this._loggerFactory);
    }
  }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
    base.ConfigureConventions(configurationBuilder);
    // Make all string properties case insensitive.
    configurationBuilder.Properties<String>().UseCollation(CaseInsensitiveCollation);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder
      // Collation info: https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html
      // More (horrible) documentation: https://www.unicode.org/reports/tr35/tr35-collation.html#Setting_Options
      // Root: English - with modifiers - comparison strength - case insensitive, accent & punctuation sensitive
      //       en      -       u        -          ks         -     level2
      .HasCollation(CaseInsensitiveCollation, locale: "en-u-ks-level2", provider: "icu", deterministic: false)
      .Entity<Tenant>(entity => {
        // Tenants must be explicitly deleted before deleting an environment
        entity.HasOne<Environment>()
          .WithMany()
          .HasForeignKey(t => t.EnvironmentId);
      })
      .Entity<Service>(entity => {
        entity.HasOne<Tenant>()
          .WithMany()
          .HasForeignKey(s => s.TenantId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<ServiceRelationship>(entity => {
        // If either service is deleted, delete the relationship
        entity.HasOne<Service>()
          .WithMany()
          .HasForeignKey(sr => sr.ServiceId)
          .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Service>()
          .WithMany()
          .HasForeignKey(sr => sr.ParentServiceId)
          .OnDelete(DeleteBehavior.Cascade);
        entity.HasKey(sr => new { sr.ServiceId, sr.ParentServiceId });
      })
      .Entity<HealthCheck>(entity => {
        entity.HasOne<Service>()
          .WithMany()
          .HasForeignKey(hc => hc.ServiceId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<VersionCheck>(entity => {
        entity.HasOne<Service>()
          .WithMany()
          .HasForeignKey(vc => vc.ServiceId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<ApiKey>(entity => {
        entity.HasOne<Tenant>()
          .WithMany()
          .HasForeignKey(k => k.TenantId)
          .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Environment>()
          .WithMany()
          .HasForeignKey(k => k.EnvironmentId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<UserPermission>(entity => {
        // Users are un-deletable, so no need to set up cascading delete
        entity.HasOne<User>()
          .WithMany()
          .HasForeignKey(up => up.UserId);
        entity.HasOne<Environment>()
          .WithMany()
          .HasForeignKey(up => up.EnvironmentId)
          .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Tenant>()
          .WithMany()
          .HasForeignKey(up => up.TenantId)
          .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(p => new {
          p.UserId,
          p.Permission
        })
          .HasDatabaseName("ix_user_permission_unique_global_scope")
          .HasFilter("environment_id IS NULL AND tenant_id IS NULL")
          .IsUnique();
        entity.HasIndex(p => new {
          p.UserId,
          p.EnvironmentId,
          p.Permission
        })
          .HasDatabaseName("ix_user_permission_unique_environment_scope")
          .HasFilter("environment_id IS NOT NULL AND tenant_id IS NULL")
          .IsUnique();
        entity.HasIndex(p => new {
          p.UserId,
          p.EnvironmentId,
          p.TenantId,
          p.Permission
        })
          .HasDatabaseName("ix_user_permission_unique_tenant_scope")
          .HasFilter("environment_id IS NOT NULL AND tenant_id IS NOT NULL")
          .IsUnique();
      })
      .Entity<HealthCheckCache>(entity => {
        entity.HasOne<ServiceHealthCache>()
          .WithMany()
          .HasForeignKey(hcc => hcc.ServiceHealthId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<ErrorReport>(entity => {
        entity.HasOne<Environment>()
          .WithMany()
          .HasForeignKey(k => k.EnvironmentId)
          .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Tenant>()
          .WithMany()
          .HasForeignKey(k => k.TenantId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<ServiceTag>(entity => {
        entity.HasOne<Service>()
          .WithMany()
          .HasForeignKey(k => k.ServiceId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<TenantTag>(entity => {
        entity.HasOne<Tenant>()
          .WithMany()
          .HasForeignKey(k => k.TenantId)
          .OnDelete(DeleteBehavior.Cascade);
      })
      .Entity<ServiceVersionCache>();
  }
}
