using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public interface IServiceConfigSource {
  /// <summary>
  ///   Retrieves an unordered list of tenant names that this source has configuration for.
  /// </summary>
  IAsyncEnumerable<String> GetTenantsAsync(CancellationToken cancellationToken);

  /// <summary>
  ///   Retrieves an ordered list of <see cref="ServiceHierarchyConfiguration" /> for the specified
  ///   tenant.
  /// </summary>
  IAsyncEnumerable<ServiceHierarchyConfiguration> GetConfigurationLayersAsync(
    String tenant,
    CancellationToken cancellationToken);
}
