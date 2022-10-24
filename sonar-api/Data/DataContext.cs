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
      .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

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
      .Entity<ServiceRelationship>(entity => {
        entity.HasOne<Service>()
          .WithMany().HasForeignKey(sr => sr.ServiceId);
        entity.HasOne<Service>()
          .WithMany().HasForeignKey(sr => sr.ParentServiceId);
        entity.HasKey(sr => new { sr.ServiceId, sr.ParentServiceId });
      })
      .Entity<Service>(entity => {
        entity.HasOne<Tenant>()
          .WithMany()
          .HasForeignKey(s => s.TenantId);
      })
      .Entity<Tenant>(entity => {
        entity.HasOne<Environment>()
          .WithMany()
          .HasForeignKey(t => t.EnvironmentId);
      });
  }
}
