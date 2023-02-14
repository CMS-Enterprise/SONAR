using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("health_check_cache")]
[Index(propertyNames: new[] { nameof(ServiceHealthId), nameof(HealthCheck) }, IsUnique = true)]
public class HealthCheckCache {
  public Guid Id { get; init; }
  public Guid ServiceHealthId { get; init; }
  public String HealthCheck { get; init; }
  public HealthStatus Status { get; init; }

  public HealthCheckCache(
    Guid id,
    Guid serviceHealthId,
    String healthCheck,
    HealthStatus status) {
    this.Id = id;
    this.ServiceHealthId = serviceHealthId;
    this.HealthCheck = healthCheck;
    this.Status = status;
  }

  public static HealthCheckCache New(
    Guid serviceHealthId,
    String healthCheck,
    HealthStatus status) =>
    new HealthCheckCache(Guid.Empty, serviceHealthId, healthCheck, status);
}
