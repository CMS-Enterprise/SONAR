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
  /// <remarks>
  ///   Each tenant in the result must be unique (case insensitive).
  /// </remarks>
  IAsyncEnumerable<String> GetTenantsAsync(CancellationToken cancellationToken);

  /// <summary>
  ///   Retrieves an ordered list of <see cref="ServiceHierarchyConfiguration" /> for the specified
  ///   tenant.
  /// </summary>
  /// <exception cref="InvalidConfigurationException">
  ///   An error occurred reading or deserializing a configuration layer.
  /// </exception>
  IAsyncEnumerable<ServiceHierarchyConfiguration> GetConfigurationLayersAsync(
    String tenant,
    CancellationToken cancellationToken);
}
