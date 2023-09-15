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
  /// <exception cref="ServiceConfigSourceException">
  ///   An error occurred reading or deserializing a configuration layer.
  /// </exception>
  IAsyncEnumerable<ServiceHierarchyConfiguration> GetConfigurationLayersAsync(
    String tenant,
    CancellationToken cancellationToken);

  // Alternative: add a method to get raw config data
  // IAsyncEnumerable<String> GetRawConfigurationLayers(String tenant,
  //   CancellationToken cancellationToken);
}


public class ServiceConfigSourceException : Exception {
  // TODO: add a legitimate constructor, support serialization (google how to implement a custom C# exception including serialization support or see ProblemDetailException as an example)
  public String LayerDescription { get; init; } = String.Empty;
  public Int32 LayerNumber { get; init; }
  public String? RawConfig { get; init;}
}
