using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.Helpers;

/// <summary>
///   Sends of an Error report comprising of <see cref="ErrorReportDetails"/>
///   to the API.
/// </summary>
/// <exception cref="ApiException">
///   Failed to create error report in SONAR API.
/// </exception>
public interface IErrorReportsHelper {
  Task CreateErrorReport(
    String environment,
    ErrorReportDetails reportDetails,
    CancellationToken cancellationToken);
}
