using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models;

public record ServiceConfiguration : IValidatableObject {

  public ServiceConfiguration(
    String name,
    String displayName,
    String? description = null,
    Uri? url = null,
    IImmutableList<HealthCheckModel>? healthChecks = null,
    IImmutableList<VersionCheckModel>? versionChecks = null,
    IImmutableSet<String>? children = null,
    IImmutableDictionary<String, String?>? tags = null,
    IImmutableList<AlertingRuleConfiguration>? alertingRules = null,
    IImmutableList<ScheduledMaintenanceConfiguration>? scheduledMaintenances = null) {

    this.Name = name;
    this.DisplayName = displayName;
    this.Description = description;
    this.Url = url;
    this.HealthChecks = healthChecks;
    this.VersionChecks = versionChecks;
    this.Children = children;
    this.Tags = tags;
    this.AlertingRules = alertingRules;
    this.ScheduledMaintenances = scheduledMaintenances;
  }

  [StringLength(100)]
  [Required]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  public String Name { get; init; }

  [Required]
  public String DisplayName { get; init; }

  public String? Description { get; init; }

  public Uri? Url { get; init; }

  public IImmutableList<HealthCheckModel>? HealthChecks { get; init; }

  public IImmutableList<VersionCheckModel>? VersionChecks { get; init; }

  public IImmutableSet<String>? Children { get; init; }

  public IImmutableDictionary<String, String?>? Tags { get; init; }

  public IImmutableList<AlertingRuleConfiguration>? AlertingRules { get; init; }

  public IImmutableList<ScheduledMaintenanceConfiguration>? ScheduledMaintenances { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>();

    var alertingRuleNames = (this.AlertingRules?.Select(r => r.Name) ?? ImmutableList<String>.Empty)
      .ToImmutableList();
    var distinctAlertingRuleNames = new HashSet<String>(alertingRuleNames, StringComparer.OrdinalIgnoreCase)
      .ToImmutableHashSet();

    if (distinctAlertingRuleNames.Count != alertingRuleNames.Count) {
      validationResults.Add(new ValidationResult(
        errorMessage: "One or more alerting rules have the same name.",
        memberNames: new[] { nameof(this.AlertingRules) }));
    }

    return validationResults;
  }
}
