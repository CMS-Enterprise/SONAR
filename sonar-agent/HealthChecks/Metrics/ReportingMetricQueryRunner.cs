using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public class ReportingMetricQueryRunner : IMetricQueryRunner {
  private readonly IMetricQueryRunner _runnerImplementation;
  private readonly Func<(IDisposable, ISonarClient)> _sonarClientFactory;

  public ReportingMetricQueryRunner(
    IMetricQueryRunner runnerImplementation,
    Func<(IDisposable, ISonarClient)> sonarClientFactory) {

    this._runnerImplementation = runnerImplementation;
    this._sonarClientFactory = sonarClientFactory;
  }

  public async Task<IImmutableList<(DateTime Timestamp, Decimal Value)>?> QueryRangeAsync(
    HealthCheckIdentifier healthCheck,
    String expression,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {

    var results =
      await this._runnerImplementation.QueryRangeAsync(
        healthCheck,
        expression,
        start,
        end,
        cancellationToken);

    var (httpClient, sonarClient) = this._sonarClientFactory();
    try {
      if (results != null) {
        await sonarClient.RecordHealthCheckDataAsync(
          healthCheck.Environment,
          healthCheck.Tenant,
          healthCheck.Service,
          new ServiceHealthData(
            ImmutableDictionary<String, IImmutableList<(DateTime Timestamp, Double Value)>>.Empty.Add(
              healthCheck.Name,
              results.Select(tpl => (tpl.Timestamp, (Double)tpl.Value)).ToImmutableList()
            )
          ),
          cancellationToken
        );
      }

      return results;
    } finally {
      httpClient.Dispose();
    }
  }
}
