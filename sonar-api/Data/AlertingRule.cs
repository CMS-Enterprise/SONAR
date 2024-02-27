using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("alerting_rule")]
[Index(nameof(ServiceId), nameof(Name), IsUnique = true)]
public class AlertingRule {
  public Guid Id { get; init; }
  public Guid ServiceId { get; init; }
  public Guid AlertReceiverId { get; init; }

  [StringLength(100)]
  public String Name { get; init; }

  public HealthStatus Threshold { get; init; }
  public Int32 Delay { get; init; }

  public AlertingRule(
    Guid id,
    Guid serviceId,
    Guid alertReceiverId,
    String name,
    HealthStatus threshold,
    Int32 delay) {

    this.Id = id;
    this.ServiceId = serviceId;
    this.AlertReceiverId = alertReceiverId;
    this.Name = name;
    this.Threshold = threshold;
    this.Delay = delay;
  }

  public static AlertingRule New(
    Guid serviceId,
    Guid alertReceiverId,
    AlertingRuleConfiguration alertingRuleConfiguration) =>
    new AlertingRule(
      id: default,
      serviceId,
      alertReceiverId,
      alertingRuleConfiguration.Name,
      alertingRuleConfiguration.Threshold,
      alertingRuleConfiguration.Delay);
}
