using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

    var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    foreach (var source in this._sources) {
      await foreach (var tenant in source.GetTenantsAsync(cancellationToken)) {
        // Only return each tenant once, even if multiple sources contain configuration.
        if (seen.Add(tenant)) {
          yield return tenant;
        }
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
