using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Data;

[Table("error_detail")]
public class ErrorDetail {
  [Key]
  public Guid Id { get; init; }
  public DateTime Timestamp { get; set; }
  public Guid EnvironmentId { get; set; }
  public Guid? TenantId { get; set; }
  public String? ServiceName { get; set; }
  public String? HealthCheckName { get; set; }
  public ErrorLevel Level { get; set; }
  public ErrorType Type { get; set; }
  public String Message { get; set; }
  public String? Configuration { get; set; }
  public String? StackTrace { get; set; }

  public ErrorDetail(
    Guid id,
    DateTime timestamp,
    Guid environmentId,
    Guid? tenantId,
    String? serviceName,
    String? healthCheckName,
    ErrorLevel level,
    ErrorType type,
    String message,
    String? configuration,
    String? stackTrace) {

    this.Id = id;
    this.Timestamp = timestamp;
    this.EnvironmentId = environmentId;
    this.TenantId = tenantId;
    this.ServiceName = serviceName;
    this.HealthCheckName = healthCheckName;
    this.Level = level;
    this.Type = type;
    this.Message = message;
    this.Configuration = configuration;
    this.StackTrace = stackTrace;
  }
}
