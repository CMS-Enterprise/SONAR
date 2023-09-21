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
      this._logger.LogWarning(e,
        "Failed to create error report in SONAR API, Code: {StatusCode}, Message: {_Message}",
        e.StatusCode,
        e.Message);
    } catch (Exception e) when (e is not OperationCanceledException or OutOfMemoryException) {
      // Handle all exceptions raised when attempting to report errors to SONAR API so that we do
      // not mask the underlying error
      this._logger.LogWarning(e,
        "Unexpected error reporting error to SONAR API, Message: {_Message}",
        e.Message);
    } finally {
      conn.Dispose();
    }
  }
}
