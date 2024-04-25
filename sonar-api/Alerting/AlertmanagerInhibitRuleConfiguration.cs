using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Alerting;

internal record AlertmanagerInhibitRuleConfiguration(
  [property:JsonPropertyName("source_matchers")]
  ImmutableList<String> SourceMatchers,
  [property:JsonPropertyName("target_matchers")]
  ImmutableList<String> TargetMatchers,
  ImmutableList<String> Equal
  );
