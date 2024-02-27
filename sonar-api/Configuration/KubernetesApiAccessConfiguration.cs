using System;

namespace Cms.BatCave.Sonar.Configuration;

public record KubernetesApiAccessConfiguration(
  Boolean IsEnabled = false,
  Boolean IsInCluster = false,
  String TargetNamespace = "sonar");
