using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;

public class ReportingMetricQueryRunner : IMetricQueryRunner {
  private readonly IMetricQueryRunner _runnerImplementation;
  private readonly Func<(IDisposable, ISonarClient)> _sonarClientFactory;
  private readonly ILogger<ReportingMetricQueryRunner> _logger;

  public ReportingMetricQueryRunner(
    IMetricQueryRunner runnerImplementation,
    Func<(IDisposable, ISonarClient)> sonarClientFactory,
    ILogger<ReportingMetricQueryRunner> logger) {

    this._runnerImplementation = runnerImplementation;
    this._sonarClientFactory = sonarClientFactory;
    this._logger = logger;
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
        this._logger.LogTrace(
          "Reporting {MetricCount} metric values for health check {HealthCheck}",
          results.Count,
          healthCheck
        );

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
    } catch (ApiException ex) {
      this._logger.LogError(
        ex,
        "SONAR API Returned an non-success status code when attempting to report {MetricCount} metric values for health check {HealthCheck}: {Message}",
        results?.Count,
        healthCheck,
        ex.Message
      );
    } catch (HttpRequestException ex) {
      this._logger.LogError(
        ex,
        "An network error occurred attempting when attempting to report {MetricCount} metric values for health check {HealthCheck}: {Message}",
        results?.Count,
        healthCheck,
        ex.Message
      );
    } catch (TaskCanceledException ex) {
      if (cancellationToken.IsCancellationRequested) {
        throw;
      } else {
        this._logger.LogError("HTTP request timed out attempting to report metrics to SONAR API");
      }
    } finally {
      httpClient.Dispose();
    }

    return results;
  }
}
