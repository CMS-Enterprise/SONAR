using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public class LocalFileServiceConfigSource : IServiceConfigSource {
  private readonly String _tenant;
  private readonly ImmutableList<String> _filePaths;

  public LocalFileServiceConfigSource(
    String tenant,
    IEnumerable<String> filePaths) {
    this._tenant = tenant;
    this._filePaths = filePaths.ToImmutableList();
  }

  public IAsyncEnumerable<String> GetTenantsAsync(CancellationToken cancellationToken) {
    return new[] { this._tenant }.ToAsyncEnumerable();
  }

  public async IAsyncEnumerable<ServiceHierarchyConfiguration> GetConfigurationLayersAsync(
    String tenant,
    [EnumeratorCancellation] CancellationToken cancellationToken) {

    if (tenant == this._tenant) {
      foreach (var file in this._filePaths) {
        await using var inputStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(inputStream);
        var fileContent = await reader.ReadToEndAsync(cancellationToken);

        yield return JsonServiceConfigDeserializer.Deserialize(fileContent);
      }
    }
  }
}
