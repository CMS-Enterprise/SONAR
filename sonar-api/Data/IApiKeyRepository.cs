using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Data;

public interface IApiKeyRepository {
  Task<ApiKeyConfiguration> AddAsync(ApiKeyDetails apiKey, CancellationToken cancelToken);
  Task<Guid> DeleteAsync(Guid id, CancellationToken cancelToken);
  Task<List<ApiKeyConfiguration>> GetKeysAsync(CancellationToken cancelToken);
  Task<List<ApiKeyConfiguration>> GetEnvKeysAsync(Guid environmentId, CancellationToken cancelToken);
  Task<List<ApiKeyConfiguration>> GetTenantKeysAsync(ApiKey encKey, CancellationToken cancelToken);

  Task<ApiKey> GetApiKeyFromApiKeyIdAsync(Guid keyId, CancellationToken cancellationToken);
  Task<ApiKey> GetApiKeyFromEncKeyAsync(String encKey, CancellationToken cancelToken);
}
