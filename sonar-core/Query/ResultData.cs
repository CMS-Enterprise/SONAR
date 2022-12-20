using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Query;

public record ResultData(
  [property:JsonPropertyName("metric")]
  IImmutableDictionary<String, String> Labels,
  (Decimal Timestamp, String Value)? Value,
  IImmutableList<(Decimal Timestamp, String Value)>? Values);
