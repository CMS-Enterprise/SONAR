using System;

namespace Cms.BatCave.Sonar.Agent.Configuration;

public record HealthCheckQueueProcessorConfiguration(Int32 MaximumConcurrency = 3) {
}
