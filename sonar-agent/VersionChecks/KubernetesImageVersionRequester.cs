using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Models;

namespace Cms.BatCave.Sonar.Agent.VersionChecks;


public class KubernetesImageVersionRequester : IVersionRequester<KubernetesVersionCheckDefinition> {

  private readonly Kubernetes _client;
  private const String DefaultVersion = "Unknown";
  public KubernetesImageVersionRequester(Kubernetes kubeClient) {
    this._client = kubeClient;
  }
  public async Task<VersionResponse> GetVersionAsync(KubernetesVersionCheckDefinition versionCheckDefinition, CancellationToken ct = default) {
    var requestTimestamp = DateTime.UtcNow;
    var version = DefaultVersion;

    try {
      V1Container? container;
      switch (versionCheckDefinition.ResourceType) {
        case KubernetesResourceType.Deployment:
          var deployment = await this._client.ReadNamespacedDeploymentAsync(versionCheckDefinition.ResourceName, versionCheckDefinition.ResourceNamespace, cancellationToken: ct);
          container = deployment.Spec.Template.Spec.Containers.First(c => c.Name == versionCheckDefinition.ContainerName);
          if (container != null) {
            version = container.Image;
          }
          break;

        case KubernetesResourceType.StatefulSet:
          var statefulSet = await this._client.ReadNamespacedStatefulSetAsync(versionCheckDefinition.ResourceName, versionCheckDefinition.ResourceNamespace, cancellationToken: ct);
          container = statefulSet.Spec.Template.Spec.Containers.First(c => c.Name == versionCheckDefinition.ContainerName);
          if (container != null) {
            version = container.Image;
          }
          break;

        case KubernetesResourceType.DaemonSet:
          var daemonSet = await this._client.ReadNamespacedDaemonSetAsync(versionCheckDefinition.ResourceName, versionCheckDefinition.ResourceNamespace, cancellationToken: ct);
          container = daemonSet.Spec.Template.Spec.Containers.First(c => c.Name == versionCheckDefinition.ContainerName);
          if (container != null) {
            version = container.Image;
          }
          break;

        default:
          break;
      }
    } catch (Exception e) {
      throw new VersionRequestException(message: "Version request failed.", e);
    }

    if (version == DefaultVersion) {
      throw new VersionRequestException(
        message: $"Kubernetes {versionCheckDefinition.ResourceName} not found in {versionCheckDefinition.ResourceNamespace}");
    }
    return new VersionResponse(requestTimestamp, version);
  }

}
