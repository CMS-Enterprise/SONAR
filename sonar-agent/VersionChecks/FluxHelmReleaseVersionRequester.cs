using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.K8sCustomResources;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Autorest;

namespace Cms.BatCave.Sonar.Agent.VersionChecks;

public class FluxHelmReleaseVersionRequester : IVersionRequester<FluxHelmReleaseVersionCheckDefinition> {
  private readonly GenericClient _helmReleaseClientV2Beta1;
  private readonly GenericClient _helmReleaseClientV2Beta2;

  public FluxHelmReleaseVersionRequester(
    IKubernetes kubeClient) {

    this._helmReleaseClientV2Beta1 = new GenericClient(kubeClient,
      "helm.toolkit.fluxcd.io", "v2beta1", "helmreleases");

    this._helmReleaseClientV2Beta2 = new GenericClient(kubeClient,
      "helm.toolkit.fluxcd.io", "v2beta2", "helmreleases");
  }

  public async Task<VersionResponse> GetVersionAsync(
    FluxHelmReleaseVersionCheckDefinition versionCheckDefinition,
    CancellationToken ct = default) {

    var requestTimestamp = DateTime.UtcNow;
    FluxHelmRelease? helmRelease;
    CustomResourceList<FluxHelmRelease>? helmReleases;

    try {
      try {
        // Use Flux HelmRelease Controller v1beta2
        helmReleases = await this._helmReleaseClientV2Beta1
          .ListNamespacedAsync<CustomResourceList<FluxHelmRelease>>(versionCheckDefinition.K8sNamespace, ct);
      } catch (HttpOperationException) {
        // Use Flux HelmRelease Controller v2beta2
        helmReleases = await this._helmReleaseClientV2Beta2
          .ListNamespacedAsync<CustomResourceList<FluxHelmRelease>>(versionCheckDefinition.K8sNamespace, ct);
      }

      helmRelease = helmReleases.Items.SingleOrDefault(k =>
        k.Metadata.Name.Equals(versionCheckDefinition.HelmRelease, StringComparison.OrdinalIgnoreCase)
      );

      if (helmRelease == null) {
        throw new VersionRequestException(
          message: $"HelmRelease {versionCheckDefinition.HelmRelease} not found in {versionCheckDefinition.K8sNamespace}");
      }

    } catch (Exception e) {
      throw new VersionRequestException(message: "Version request failed.", e);
    }

    return new VersionResponse(requestTimestamp, helmRelease.Status.LastAppliedRevision ?? "Unknown");
  }
}
