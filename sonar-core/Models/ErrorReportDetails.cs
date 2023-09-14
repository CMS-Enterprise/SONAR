using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ErrorReportDetails {
  public ErrorReportDetails(
    DateTime timestamp,
    String? tenant,
    String? service,
    String? healthCheckName,
    AgentErrorLevel level,
    AgentErrorType type,
    String message,
    String? configuration,
    String? stackTrace) {

    this.Timestamp = timestamp;
    this.Tenant = tenant;
    this.Service = service;
    this.HealthCheckName = healthCheckName;
    this.Level = level;
    this.Type = type;
    this.Message = message;
    this.Configuration = configuration;
    this.StackTrace = stackTrace;
  }

  [Required]
  public DateTime Timestamp { get; init; }

  public String? Tenant { get; init; }

  public String? Service { get; init; }

  public String? HealthCheckName { get; init; }

  [Required]
  public AgentErrorLevel Level { get; init; }

  [Required]
  public AgentErrorType Type { get; init; }

  [Required]
  public String Message { get; init; }

  public String? Configuration { get; init; }

  public String? StackTrace { get; init; }

}
