using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("service_health_cache")]
[Index(nameof(Environment), nameof(Tenant), nameof(Service) , IsUnique = true)]
public class ServiceHealthCache {
  public Guid Id { get; init; }
  public String Environment { get; init; }
  public String Tenant { get; init; }
  public String Service { get; init; }
  public DateTime Timestamp { get; init; }
  public HealthStatus AggregateStatus { get; init; }

  public ServiceHealthCache(
    Guid id,
    String environment,
    String tenant,
    String service,
    DateTime timestamp,
    HealthStatus aggregateStatus) {
    this.Id = id;
    this.Environment = environment;
    this.Tenant = tenant;
    this.Service = service;
    this.Timestamp = timestamp;
    this.AggregateStatus = aggregateStatus;
  }

  public static ServiceHealthCache New(
    String environment,
    String tenant,
    String service,
    DateTime timestamp,
    HealthStatus aggregateStatus) =>
    new ServiceHealthCache(Guid.Empty, environment, tenant, service, timestamp, aggregateStatus);
}
