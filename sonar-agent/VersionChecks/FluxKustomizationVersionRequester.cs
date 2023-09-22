using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.K8sCustomResources;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Models;
using k8s;

namespace Cms.BatCave.Sonar.Agent.VersionChecks;

public class FluxKustomizationVersionRequester : IVersionRequester<FluxKustomizationVersionCheckDefinition> {

  private readonly GenericClient _kustomizationClient;

  public FluxKustomizationVersionRequester(
    IKubernetes kubeClient) {

    this._kustomizationClient = new GenericClient(kubeClient,
      "kustomize.toolkit.fluxcd.io", "v1beta2", "kustomizations");
  }

  public async Task<VersionResponse> GetVersionAsync(
    FluxKustomizationVersionCheckDefinition versionCheckDefinition,
    CancellationToken ct = default) {

    var requestTimestamp = DateTime.UtcNow;

    FluxKustomization? kustomization;

    try {
      var kustomizations = await this._kustomizationClient
        .ListNamespacedAsync<CustomResourceList<FluxKustomization>>(versionCheckDefinition.K8sNamespace, ct);

      kustomization = kustomizations.Items.SingleOrDefault(k =>
        k.Metadata.Name.Equals(versionCheckDefinition.Kustomization, StringComparison.OrdinalIgnoreCase)
      );
    } catch (Exception e) {
      throw new VersionRequestException(message: "Version request failed.", e);
    }

    if (kustomization == null) {
      throw new VersionRequestException(
        message: $"Kustomization {versionCheckDefinition.Kustomization} not found in {versionCheckDefinition.K8sNamespace}");
    }

    return new VersionResponse(requestTimestamp, kustomization.Status.LastAppliedRevision ?? "Unknown");
  }
}
