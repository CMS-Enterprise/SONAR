using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class TenantDataHelper {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<HealthCheck> _healthChecksTable;

  public TenantDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<HealthCheck> healthChecksTable) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._healthChecksTable = healthChecksTable;
  }

  public async Task<Tenant> FetchExistingTenantAsync(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    // Check if the environment and tenant exist
    var result =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenantName),
          leftKeySelector: e => e.Id,
          rightKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new { Environment = env, Tenant = t })
        .SingleOrDefaultAsync(cancellationToken);

    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (result.Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    }

    return result.Tenant;
  }

  public async Task<IList<Tenant>> FetchAllExistingTenantsAsync(
    CancellationToken cancellationToken) {

    //TODO use left join
    var test =
      await this._environmentsTable
        .Join(
          this._tenantsTable,
           outerKeySelector: e => e.Id,
           innerKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new {
            Environment = env,
            Tenants = t
          })
        .ToListAsync(cancellationToken);

    var result = test.GroupBy(row => row.Environment.Id);

     /*
    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment));
    } else if (result.Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant));
    }  */

     //TODO Add entity comparison ticket
     //TODO Handle case where tenant is null

     /*
    return result
      .Select(g => (g.First().Environment, (IList<Tenant>)g.Select(row => row.Tenant!)
        .ToImmutableList()));
        */
     /*
     return result.Select(g =>  (IList<Tenant>)g.Select(row => row.Tenants).ToImmutableList());
     */

     return test.Select(row => row.Tenants).ToImmutableList();

  }
}
