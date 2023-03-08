using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.HealthChecks;

/// <summary>
///   An interface for implementing asynchronous health checks evaluator.
/// </summary>
/// <remarks>
///   The implementations of all methods on this interface should be thread safe.
/// </remarks>
/// <typeparam name="TDefinition">
///   The type of <see cref="HealthCheckDefinition" /> supported by the evaluator.
/// </typeparam>
public interface IHealthCheckEvaluator<in TDefinition> where TDefinition : HealthCheckDefinition {
  /// <summary>
  ///   Evaluates the specified health check definition to determine the <see cref="HealthStatus" /> of
  ///   the associated service.
  /// </summary>
  /// <param name="name">
  ///   An identifying name for the health check. Typically a combination of the service name and health
  ///   check name (i.e. "my-service/my-health-check").
  /// </param>
  /// <param name="definition">
  ///   The definition of the health check.
  /// </param>
  Task<HealthStatus> EvaluateHealthCheckAsync(
    String name,
    TDefinition definition,
    CancellationToken cancellationToken = default);
}
