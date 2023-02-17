using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.HealthChecks;

public interface IHealthCheckEvaluator<in TDefinition> where TDefinition : HealthCheckDefinition {
  Task<HealthStatus> EvaluateHealthCheckAsync(
    String name,
    TDefinition definition,
    CancellationToken cancellationToken = default);
}
