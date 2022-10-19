using Cms.BatCave.Sonar.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cms.BatCave.Sonar.Data;

public class DataContext : DbContext {
  private readonly IOptions<DatabaseConfiguration> _configuration;

  public DataContext(IOptions<DatabaseConfiguration> configuration) {
    this._configuration = configuration;
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
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder
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
