namespace Cms.BatCave.Sonar.Enumeration;

public enum VersionCheckType {
  FluxKustomization = 0,
  FluxHelmRelease,
  HttpResponseBody,
  KubernetesImage,
}
