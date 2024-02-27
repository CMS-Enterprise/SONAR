using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Alerting;

public record PrometheusAlertingRule(
  String Alert,
  [property:JsonPropertyName("expr")]
  String Expression,
  [property:JsonConverter(typeof(PrometheusTimeSpanConverter))]
  TimeSpan For,
  ImmutableDictionary<String, String> Labels,
  ImmutableDictionary<String, String> Annotations);
