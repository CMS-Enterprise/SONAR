using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Alerting;

public record AlertmanagerRoute(
  String Receiver,
  IImmutableList<String>? Matchers = null,
  [property:JsonPropertyName("group_by")]
  IImmutableSet<String>? GroupBy = null,
  Boolean? Continue = null,
  [property:JsonPropertyName("group_wait")]
  [property:JsonConverter(typeof(PrometheusTimeSpanConverter))]
  TimeSpan? GroupWait = null,
  [property:JsonPropertyName("group_interval")]
  [property:JsonConverter(typeof(PrometheusTimeSpanConverter))]
  TimeSpan? GroupInterval = null,
  [property:JsonPropertyName("repeat_interval")]
  [property:JsonConverter(typeof(PrometheusTimeSpanConverter))]
  TimeSpan? RepeatInterval = null,
  IImmutableList<AlertmanagerRoute>? Routes = null);
