using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record MetricData(
  String MetricName,
  MetricType MetricType,
  String HelpText,
  ImmutableList<(DateTime timestamp, Double value)> TimeSeries,
  ImmutableDictionary<String, String> Labels
);
