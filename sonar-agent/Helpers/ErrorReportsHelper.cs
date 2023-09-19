using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent;

public class ErrorReportsHelper {
  private readonly Func<(IDisposable, ISonarClient)> _sonarClientFactory;
  private readonly ILogger<ErrorReportsHelper> _logger;

  public ErrorReportsHelper(
    Func<(IDisposable, ISonarClient)> sonarClientFactory,
    ILogger<ErrorReportsHelper> logger) {

    this._sonarClientFactory = sonarClientFactory;
    this._logger = logger;
  }

  public async Task CreateErrorReport(
    String environment,
    ErrorReportDetails reportDetails,
    CancellationToken cancellationToken) {

    var (conn, client) = this._sonarClientFactory();
    try {
      await client.CreateErrorReportAsync(
        environment,
        reportDetails,
        cancellationToken);
    } catch (ApiException e) {
      this._logger.LogError(e,
        "Failed to create error report in SONAR API, Code: {StatusCode}, Message: {Message}",
        e.StatusCode,
        e.Message);
    } finally {
      conn.Dispose();
    }
  }
}
