using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record UptimeModel {

  public UptimeModel(
    String name,
    Double percentUptime,
    TimeSpan totalUptime,
    TimeSpan currentUptime,
    TimeSpan unknownDuration,
    IImmutableList<UptimeModel> children) {

    this.Name = name;
    this.PercentUptime = percentUptime;
    this.TotalUptime = totalUptime;
    this.CurrentUptime = currentUptime;
    this.UnknownDuration = unknownDuration;
    this.Children = children;
  }

  [Required]
  public String Name { get; init; }

  [Required]
  public Double PercentUptime { get; init; }

  [Required]
  public TimeSpan TotalUptime { get; init; }

  [Required]
  public TimeSpan CurrentUptime { get; init; }

  [Required]
  public TimeSpan UnknownDuration { get; init; }

  [Required]
  public IImmutableList<UptimeModel> Children { get; init; }
}
