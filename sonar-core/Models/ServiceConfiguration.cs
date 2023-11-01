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
    IImmutableList<VersionCheckModel>? versionChecks = null,
    IImmutableSet<String>? children = null,
    IImmutableDictionary<String, String?>? tags = null) {

    this.Name = name;
    this.DisplayName = displayName;
    this.Description = description;
    this.Url = url;
    this.HealthChecks = healthChecks;
    this.VersionChecks = versionChecks;
    this.Children = children;
    this.Tags = tags;
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
}
