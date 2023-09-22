using System;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace Cms.BatCave.Sonar.Agent.K8sCustomResources;

public class FluxKustomization : CustomResource<KustomizationSpec, KustomizationStatus> {
  public override String ToString() {
    var labels = "{";
    foreach (var kvp in Metadata.Labels) {
      labels += kvp.Key + " : " + kvp.Value + ", ";
    }

    labels = labels.TrimEnd(',', ' ') + "}";

    return $"{Metadata.Name} (Labels: {labels}), " +
      $"Spec: {Spec.Interval}, {Spec.Path}, {Spec.KSourceRef}, {Spec.Prune}, " +
      $"Status: {Status.LastAppliedRevision}";
  }
}

public class KustomizationSourceRef {
  [JsonPropertyName("kind")]
  public String Kind { get; set; } = String.Empty;

  [JsonPropertyName("name")]
  public String Name { get; set; } = String.Empty;

  [JsonPropertyName("namespace")]
  public String Namespace { get; set; } = String.Empty;
}

public class KustomizationSpec {
  [JsonPropertyName("interval")]
  public String Interval { get; set; } = String.Empty;

  [JsonPropertyName("path")]
  public String Path { get; set; } = String.Empty;

  [JsonPropertyName("sourceRef")]
  public KustomizationSourceRef KSourceRef { get; set; } = new KustomizationSourceRef();

  [JsonPropertyName("prune")]
  public Boolean Prune { get; set; } = false;
}

public class KustomizationStatus : V1Status {
  [JsonPropertyName("lastAppliedRevision")]
  public String? LastAppliedRevision { get; set; }
}
