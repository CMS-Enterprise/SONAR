using System.Collections.Generic;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace Cms.BatCave.Sonar.Agent.K8sCustomResources;

public abstract class CustomResource : KubernetesObject, IMetadata<V1ObjectMeta> {
  [JsonPropertyName("metadata")]
  public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
}

public abstract class CustomResource<TSpec, TStatus> : CustomResource
  where TSpec : new()
  where TStatus : new() {
  [JsonPropertyName("spec")]
  public TSpec Spec { get; set; } = new TSpec();

  [JsonPropertyName("status")] public TStatus Status { get; set; } = new TStatus();
}

public class CustomResourceList<T> : KubernetesObject
  where T : CustomResource {
  public V1ListMeta Metadata { get; set; } = new V1ListMeta();
  public List<T> Items { get; set; } = new List<T>();
}
