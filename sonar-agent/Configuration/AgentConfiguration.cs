using System;
namespace Cms.BatCave.Sonar.Configuration;

public record AgentConfiguration(
  String DefaultTenant,
  Boolean InClusterConfig = false,
  Double AgentInterval = 10);
