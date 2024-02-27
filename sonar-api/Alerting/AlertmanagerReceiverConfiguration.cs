using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Alerting;

internal record AlertmanagerReceiverConfiguration(
  String Name,
  [property:JsonPropertyName("email_configs")]
  [property:JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  ImmutableList<AlertmanagerReceiverEmailConfig>? EmailConfigs = null);
