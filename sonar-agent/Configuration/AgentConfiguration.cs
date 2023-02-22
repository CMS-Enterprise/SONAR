using System;
namespace Cms.BatCave.Sonar.Configuration;

public record AgentConfiguration(
  String DefaultTenant,
  Double AgentInterval = 10);
