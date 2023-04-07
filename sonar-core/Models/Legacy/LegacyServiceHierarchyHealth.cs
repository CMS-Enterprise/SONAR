using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Models.Legacy;

public record LegacyServiceHierarchyHealth(
  [property: JsonPropertyName("service_name")]
  String Name,
  [property: JsonPropertyName("product_name")]
  String? DisplayName,
  [property: JsonPropertyName("description")]
  String? Description,
  [property: JsonPropertyName("aggregate_severity")]
  LegacyHealthStatus Status,
  [property: JsonPropertyName("service_url")]
  String? Url,
  [property: JsonPropertyName("children")]
  IImmutableList<LegacyServiceHierarchyHealth> Children) {

  [Obsolete("Color is a UI concern and can be determined based on status.")]
  [JsonPropertyName("aggregate_color")]
  public String StatusColor {
    get {
      return this.Status switch {
        LegacyHealthStatus.Operational => "#99D18B",
        LegacyHealthStatus.Degraded => "#FFE98C",
        LegacyHealthStatus.Unresponsive => "#B50101",
        _ => throw new ArgumentOutOfRangeException()
      };
    }
  }

  [Obsolete("This field doesn't have any semantic meaning besides being a numeric equivalent to aggregate_severity.")]
  [JsonPropertyName("aggregate_value")]
  public Int32 StatusValue {
    get {
      return this.Status switch {
        LegacyHealthStatus.Operational => 100,
        LegacyHealthStatus.Degraded => 50,
        LegacyHealthStatus.Unresponsive => 0,
        _ => throw new ArgumentOutOfRangeException()
      };
    }
  }
}
