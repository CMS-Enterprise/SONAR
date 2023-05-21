using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Controllers;

public interface ISonarKeyRepository {
  ApiKeyConfiguration GetKey(Guid id);
  Task<IEnumerable<ApiKeyConfiguration>> GetKeys();
  Task<ApiKeyConfiguration> Add(ApiKeyDetails apiKey);
  ApiKey Update(ApiKeyDetails apiKey);
  Task<Guid> Delete(Guid id);
}
