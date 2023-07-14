using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class EnvironmentDataHelper {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly String _sonarEnvironment;

  public EnvironmentDataHelper(
    DbSet<Environment> environmentsTable,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig) {
    this._environmentsTable = environmentsTable;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
  }

  public async Task<Environment> FetchExistingEnvAsync(
    String environmentName,
    CancellationToken cancellationToken) {

    // Check if the environment exists
    var result = await this.TryFetchEnvironmentAsync(environmentName, cancellationToken);

    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    }

    return result;
  }

  public async Task<Environment?> TryFetchEnvironmentAsync(
    String environmentName,
    CancellationToken cancellationToken) {

    return await this._environmentsTable
      .Where(e => e.Name == environmentName)
      .SingleOrDefaultAsync(cancellationToken);
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

    // Add Sonar-Local Env
    if (!result.Any(e => String.Equals(e.Name, this._sonarEnvironment, StringComparison.OrdinalIgnoreCase))) {
      result.Add(new Environment(new Guid(), this._sonarEnvironment));
    }

    return result;
  }

  public async Task<Environment> AddAsync(
    Environment environment,
    CancellationToken cancellationToken) {

    var result = await this._environmentsTable.AddAsync(
      environment,
      cancellationToken
    );

    return result.Entity;
  }

  public void Delete(Environment environment) {
    this._environmentsTable.Remove(environment);
  }
}
