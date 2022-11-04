using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Prometheus;

public record ResultData(
  [property:JsonPropertyName("metric")]
  IImmutableDictionary<String, String> Labels,
  (Int64 Timestamp, String Value)? Value,
  IImmutableList<(Int64 Timestamp, String Value)>? Values);
