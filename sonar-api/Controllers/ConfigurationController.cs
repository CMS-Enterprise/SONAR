using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/config")]
public class ConfigurationController : ControllerBase {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DbSet<ServiceRelationship> _relationshipsTable;

  public ConfigurationController(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<ServiceRelationship> relationshipsTable) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._relationshipsTable = relationshipsTable;
  }

  [HttpGet("{environment}/tenant/{tenant}")]
  [ProducesResponseType(typeof(ServiceHierarchyConfiguration), statusCode: 200)]
  public async Task<ActionResult> GetConfiguration(
    [FromRoute] String environment,
    [FromRoute] String tenant) {

    var results =
      await this._environmentsTable
        .Where(e => e.Name == environment)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenant),
          leftKeySelector: e => e.Id,
          rightKeySelector: t => t.EnvironmentId,
          resultSelector: (env, t) => new { Environment = env, Tenant = t })
        .LeftJoin(
          this._servicesTable,
          leftKeySelector: row => row.Tenant != null ? row.Tenant.Id : (Guid?)null,
          rightKeySelector: svc => svc.TenantId,
          resultSelector: (row, svc) => new {
            Environment = row.Environment,
            Tenant = row.Tenant,
            Service = svc
          })
        .ToListAsync();

    if (results.Count == 0) {
      throw new ResourceNotFoundException(nameof(Environment), environment);
    } else if (results[0].Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenant);
    }

    var serviceMap =
      results
        .Select(r => r.Service)
        .NotNull()
        .ToImmutableDictionary(svc => svc.Id);

// Linq requires ICollection.Contains for translation to SQL IN clause
#pragma warning disable CA1841
    var serviceRelationships =
      await this._relationshipsTable
        .Where(r => serviceMap.Keys.Contains(r.ParentServiceId))
        .ToListAsync();
#pragma warning restore CA1841

    var serviceRelationshipsByParent =
      serviceRelationships.ToLookup(r => r.ParentServiceId);

    return this.Ok(new ServiceHierarchyConfiguration(
      serviceMap.Values.Select(svc => new ServiceConfiguration(
        svc.Name,
        svc.DisplayName,
        svc.Description,
        svc.Url,
        serviceRelationshipsByParent[svc.Id]
          .Select(id => serviceMap[id.ServiceId].Name)
          .NullIfEmpty()
          ?.ToImmutableHashSet()
      )).ToImmutableDictionary(svc => svc.Name),
      serviceMap.Values
        .Where(svc => svc.IsRootService)
        .Select(svc => svc.Name)
        .ToImmutableHashSet()
    ));
  }
}
