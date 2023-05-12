using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record ServiceConfiguration {

  public ServiceConfiguration(
    String name,
    String displayName,
    String? description = null,
    Uri? url = null,
    IImmutableList<HealthCheckModel>? healthChecks = null,
    IImmutableSet<String>? children = null) {

    this.Name = name;
    this.DisplayName = displayName;
    this.Description = description;
    this.Url = url;
    this.HealthChecks = healthChecks;
    this.Children = children;
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

  public IImmutableSet<String>? Children { get; init; }
}
