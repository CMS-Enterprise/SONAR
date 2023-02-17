using System;

namespace Cms.BatCave.Sonar.Agent.Configuration;

public record AgentConfiguration(
  String DefaultTenant,
  Boolean InClusterConfig = false,
  Double AgentInterval = 10,
  Int32 MaximumConcurrency = 3) : HealthCheckQueueProcessorConfiguration(MaximumConcurrency);
