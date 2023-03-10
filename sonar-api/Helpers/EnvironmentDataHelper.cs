using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class EnvironmentDataHelper {
  private readonly DbSet<Environment> _environmentsTable;

  public EnvironmentDataHelper(DbSet<Environment> environmentsTable) {
    this._environmentsTable = environmentsTable;
  }

  public async Task<Environment> FetchExistingEnvAsync(
    String environmentName,
    CancellationToken cancellationToken) {

    // Check if the environment exists
    var result =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .SingleOrDefaultAsync(cancellationToken);

    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    }

    return result;
  }

  public async Task<IList<Environment>> FetchAllExistingEnvAsync(
    CancellationToken cancellationToken) {

    // Check if the environment exists
    var result =
      await this._environmentsTable
        .ToListAsync(cancellationToken);

    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment));
    }

    return result;
  }
}
