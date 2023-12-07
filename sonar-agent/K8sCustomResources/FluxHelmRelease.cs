using System;
using System.Text.Json.Serialization;
using k8s.Models;

namespace Cms.BatCave.Sonar.Agent.K8sCustomResources;
public class FluxHelmRelease : CustomResource<HelmReleaseSpec, HelmReleaseStatus> {
  public override String ToString() {
    var labels = "{";
    foreach (var kvp in Metadata.Labels) {
      labels += kvp.Key + " : " + kvp.Value + ", ";
    }

    labels = labels.TrimEnd(',', ' ') + "}";

    return $"{Metadata.Name} (Labels: {labels}), " +
      $"Spec: {Spec.Interval}, {Spec.HelmChartTemplate.HTemplateSpec.HObjectRef}, " +
      $"Status: {Status.LastAppliedRevision}";
  }
}

// Spec https://fluxcd.io/flux/components/helm/api/v2beta2/
public class HelmReleaseSpec {
  [JsonPropertyName("chart")]
  public HelmChartTemplate HelmChartTemplate { get; set; } = new HelmChartTemplate();

  [JsonPropertyName("interval")]
  public String Interval { get; set; } = String.Empty;
}

public class HelmChartTemplate {
  [JsonPropertyName("spec")]
  public HelmchartTemplateSpec HTemplateSpec { get; set; } = new HelmchartTemplateSpec();
}

public class HelmchartTemplateSpec {
  [JsonPropertyName("sourceRef")]
  public HelmChartObjectRef HObjectRef { get; set; } = new HelmChartObjectRef();
}

public class HelmChartObjectRef {
  [JsonPropertyName("kind")]
  public String Kind { get; set; } = String.Empty;

  [JsonPropertyName("name")]
  public String Name { get; set; } = String.Empty;

  [JsonPropertyName("namespace")]
  public String Namespace { get; set; } = String.Empty;
}

public class HelmReleaseStatus : V1Status {
  [JsonPropertyName("lastAppliedRevision")]
  public String? LastAppliedRevision { get; set; }
}
