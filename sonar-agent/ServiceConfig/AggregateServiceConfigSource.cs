using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public class AggregateServiceConfigSource : IServiceConfigSource {
  private readonly ImmutableList<IServiceConfigSource> _sources;

  public AggregateServiceConfigSource(IEnumerable<IServiceConfigSource> sources) {
    this._sources = sources.ToImmutableList();
  }

  public async IAsyncEnumerable<String> GetTenantsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken) {

    foreach (var source in this._sources) {
      await foreach (var tenant in source.GetTenantsAsync(cancellationToken)) {
        yield return tenant;
      }
    }
  }

  public async IAsyncEnumerable<ServiceHierarchyConfiguration> GetConfigurationLayersAsync(
    String tenant,
    [EnumeratorCancellation] CancellationToken cancellationToken) {

    foreach (var source in this._sources) {
      await foreach (var layer in source.GetConfigurationLayersAsync(tenant, cancellationToken)) {
        yield return layer;
      }
    }
  }
}
