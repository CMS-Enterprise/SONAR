using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Prometheus;

public record ResultData(
  [property:JsonPropertyName("metric")]
  IImmutableDictionary<String, String> Labels,
  (DateTime Timestamp, String Value)? Value,
  IImmutableList<(DateTime Timestamp, String Value)>? Values);
