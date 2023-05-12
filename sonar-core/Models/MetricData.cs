using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record MetricData {

  public MetricData(
    String metricName,
    MetricType metricType,
    String helpText,
    ImmutableList<(DateTime timestamp, Double value)> timeSeries,
    ImmutableDictionary<String, String> labels) {

    this.MetricName = metricName;
    this.MetricType = metricType;
    this.HelpText = helpText;
    this.TimeSeries = timeSeries;
    this.Labels = labels;
  }

  [Required]
  public String MetricName { get; init; }

  [Required]
  public MetricType MetricType { get; init; }

  [Required]
  public String HelpText { get; init; }

  [Required]
  public ImmutableList<(DateTime timestamp, Double value)> TimeSeries { get; init; }

  [Required]
  public ImmutableDictionary<String, String> Labels { get; init; }
}
