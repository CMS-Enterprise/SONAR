using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.VersionChecks;

public interface IVersionRequester<in T> where T : VersionCheckDefinition {
  /// <summary>
  /// Get the version specified by the given <see cref="VersionCheckDefinition"/>.
  /// </summary>
  /// <param name="versionCheckDefinition">The <see cref="VersionCheckDefinition"/> of the version to get.</param>
  /// <param name="ct">A <see cref="CancellationToken"/> for the async request.</param>
  /// <returns>A <see cref="VersionResponse"/> with the version information.</returns>
  /// <exception cref="VersionRequestException">If the request could not be fulfilled.</exception>
  Task<VersionResponse> GetVersionAsync(T versionCheckDefinition, CancellationToken ct = default);
}
