using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record ServiceVersionHistory {

  public ServiceVersionHistory(
    String name,
    String displayName,
    String? description = null,
    Uri? url = null,
    IImmutableList<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>? versionHistory = null) {

    this.Name = name;
    this.DisplayName = displayName;
    this.Description = description;
    this.Url = url;
    this.VersionHistory = versionHistory;
  }

  [Required]
  public String Name { get; init; }

  [Required]
  public String DisplayName { get; init; }

  public String? Description { get; init; }

  public Uri? Url { get; init; }

  public IImmutableList<(DateTime, IImmutableList<ServiceVersionTypeInfo>)>? VersionHistory { get; init; }
}
