using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Controllers;

public interface IApiKeyRepository {
  Task<List<ApiKeyConfiguration>> GetKeysAsync(CancellationToken cancelToken);
  Task<ApiKeyConfiguration> AddAsync(ApiKeyDetails apiKey, CancellationToken cancelToken);
  Task<Guid> DeleteAsync(Guid id, CancellationToken cancelToken);
}
