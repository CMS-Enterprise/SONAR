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
  Task<List<ApiKeyConfiguration>> GetEnvKeysAsync(ApiKey encKey, CancellationToken cancelToken);
  Task<List<ApiKeyConfiguration>> GetTenantKeysAsync(ApiKey encKey, CancellationToken cancelToken);

  ApiKeyDetails GetKeyDetails(ApiKeyType apiKeyType, String? environmentName, String? tenantName);
  ApiKeyDetails GetKeyDetails(ApiKeyType apiKeyType, Guid? environmentId, Guid? tenantId);
  Task<ApiKey> GetApiKeyAsync(String encKey, CancellationToken cancelToken);
  Task<ApiKey> GetApiKeyAsync(Guid keyId, CancellationToken cancelToken);
}
