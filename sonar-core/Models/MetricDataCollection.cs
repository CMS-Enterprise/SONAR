using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record MetricDataCollection {
  public MetricDataCollection(
    IImmutableList<(DateTime timestamp, Double value)> timeSeries) {

    this.TimeSeries = timeSeries;
  }

  [Required]
  public IImmutableList<(DateTime timestamp, Double value)> TimeSeries { get; init; }
}
