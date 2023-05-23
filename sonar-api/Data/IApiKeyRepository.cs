using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Controllers;

public interface IApiKeyRepository {
  Task<IEnumerable<ApiKeyConfiguration>> GetKeysAsync();
  Task<ApiKeyConfiguration> AddAsync(ApiKeyDetails apiKey);
  Task<Guid> DeleteAsync(Guid id);
}
