using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cms.BatCave.Sonar.Exceptions;
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
      Int32 num = 0;
      foreach (var file in this._filePaths) {
        ServiceHierarchyConfiguration config;
        String fileContent = "";

        try {
          await using var inputStream = new FileStream(file, FileMode.Open, FileAccess.Read);
          using var reader = new StreamReader(inputStream);
          fileContent = await reader.ReadToEndAsync(cancellationToken);
          config = JsonServiceConfigSerializer.Deserialize(fileContent);
        } catch (InvalidConfigurationException e) {
          throw new InvalidConfigurationException(
            file,
            num,
            fileContent,
            "Error Deserializing: " + file + " Order: " + num + " " + e.Message,
            e.ErrorType,
            e
          );
        }

        yield return config;
        num++;
      }
    }
  }
}
