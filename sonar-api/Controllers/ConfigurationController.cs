using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/config")]
public class ConfigurationController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DbSet<ServiceRelationship> _relationshipsTable;

  public ConfigurationController(
    DataContext dbContext,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<ServiceRelationship> relationshipsTable) {

    this._dbContext = dbContext;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._relationshipsTable = relationshipsTable;
  }

  /// <summary>
  ///   Retrieves the configuration for the specified environment and tenant.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The tenant configuration was found and will be returned.</response>
  /// <response code="404">The specified environment or tenant was not found.</response>
  [HttpGet("{environment}/tenants/{tenant}")]
  [ProducesResponseType(typeof(ServiceHierarchyConfiguration), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetConfiguration(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    CancellationToken cancellationToken = default) {

    var (_, _, serviceMap) = await this.FetchExistingConfiguration(environment, tenant, cancellationToken);


    var serviceRelationshipsByParent =
      (await this.FetchExistingRelationships(serviceMap.Keys, cancellationToken))
      .ToLookup(r => r.ParentServiceId);

    return this.Ok(ConfigurationController.CreateServiceHierarchy(serviceMap, serviceRelationshipsByParent));
  }

  /// <summary>
  ///   Sets the configuration for a new environment or tenant.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="hierarchy">The new service hierarchy configuration.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="201">The tenant configuration successfully created.</response>
  /// <response code="409">
  ///   Configuration for the specified environment and tenant already exists. Use the PUT HTTP Method to
  ///   update this configuration.
  /// </response>
  /// <response code="400">The specified service hierarchy configuration is not valid.</response>
  [HttpPost("{environment}/tenants/{tenant}")]
  [Consumes(typeof(ServiceHierarchyConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ServiceHierarchyConfiguration), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 409)]
  public async Task<ActionResult> CreateConfiguration(
    [FromRoute] [RegularExpression("^[0-9a-zA-Z_-]+$")]
    String environment,
    [FromRoute] [RegularExpression("^[0-9a-zA-Z_-]+$")]
    String tenant,
    [FromBody] ServiceHierarchyConfiguration hierarchy,
    CancellationToken cancellationToken = default) {

    // Validation
    ConfigurationController.ValidateServiceHierarchy(hierarchy);

    ActionResult response;

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
    try {
      // Check if the environment and tenant exist
      var result =
        await this._environmentsTable
          .Where(e => e.Name == environment)
          .LeftJoin(
            this._tenantsTable.Where(t => t.Name == tenant),
            leftKeySelector: e => e.Id,
            rightKeySelector: t => t.EnvironmentId,
            resultSelector: (env, t) => new { Environment = env, Tenant = t })
          .SingleOrDefaultAsync(cancellationToken);

      // If the tenant exists, return 409 Conflict (use PUT to update)
      if (result?.Tenant != null) {
        return this.Conflict(new ProblemDetails {
          Title = $"The specified {nameof(Environment)} and {nameof(Tenant)} already exist. Use PUT to update."
        });
      }

      // If the environment does not exist, create it
      Environment environmentEntity;
      if (result == null) {
        var createdEnvironment = await this._environmentsTable.AddAsync(
          new Environment(
            Guid.Empty,
            environment),
          cancellationToken
        );

        environmentEntity = createdEnvironment.Entity;
      } else {
        environmentEntity = result.Environment;
      }

      // Create the tenant
      var createdTenant = await this._tenantsTable.AddAsync(
        new Tenant(
          Guid.Empty,
          environmentEntity.Id,
          tenant),
        cancellationToken
      );

      // Create all services defined in hierarchy
      var createdServices =
        await this._servicesTable.AddAllAsync(
          hierarchy.Services.Select(
            svc => new Service(
              Guid.Empty,
              createdTenant.Entity.Id,
              svc.Name,
              svc.DisplayName,
              svc.Description,
              svc.Url,
              hierarchy.RootServices.Contains(svc.Name, StringComparer.OrdinalIgnoreCase)
            )
          ),
          cancellationToken
        );

      // Flush changes (triggers a database error in the event of a unique constraint violation)
      await this._dbContext.SaveChangesAsync(cancellationToken);

      var servicesByName =
        createdServices.ToImmutableDictionary(keySelector: svc => svc.Name);

      // Create the service_relationships based on children lists
      var createdRelationships = await this._relationshipsTable.AddAllAsync(
        hierarchy.Services.SelectMany<ServiceConfiguration, ServiceRelationship>(svc =>
          svc.Children != null ?
            svc.Children.Select(child => new ServiceRelationship(
              servicesByName[svc.Name].Id,
              servicesByName[child].Id
            )) :
            Enumerable.Empty<ServiceRelationship>()
        ),
        cancellationToken
      );

      await this._dbContext.SaveChangesAsync(cancellationToken);

      response = this.Created(
        this.Url.Action(nameof(this.GetConfiguration), new { environment, tenant }) ?? String.Empty,
        ConfigurationController.CreateServiceHierarchy(
          servicesByName.Values.ToImmutableDictionary(svc => svc.Id),
          createdRelationships.ToLookup(rel => rel.ParentServiceId)
        )
      );

      /* Edge Cases:
       *   ✔︎ Root service name that does not exist in the services list
       *   ✔︎ Root service list casing mismatch (we're case insensitive everywhere else).
       *   ✔︎ Duplicate names in the service array (database error)
       *   ✔︎ Service name length (should be caught by ASP.Net Model Validation)
       *   ✔︎ Service name character set (should be URL safe, see JIRA ticket)
       */

      // Commit transaction
      await tx.CommitAsync(cancellationToken);
    } catch (DbUpdateException dbException)
      when (dbException.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {

      await tx.RollbackAsync(cancellationToken);

      response = this.BadRequest(new ProblemDetails {
        Title = "The specified list of services contained multiple services with the same name."
      });
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    // Return results to the caller
    return response;
  }

  /// <summary>
  ///   Updates the configuration for an existing tenant.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="hierarchy">The updated service hierarchy configuration.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The tenant configuration was found and will be returned.</response>
  /// <response code="404">The specified environment or tenant was not found.</response>
  /// <response code="400">The specified service hierarchy configuration is not valid.</response>
  [HttpPut("{environment}/tenants/{tenant}")]
  [Consumes(typeof(ServiceHierarchyConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ServiceHierarchyConfiguration), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> UpdateConfiguration(
    [FromRoute]
    String environment,
    [FromRoute]
    String tenant,
    [FromBody] ServiceHierarchyConfiguration hierarchy,
    CancellationToken cancellationToken = default) {

    // Validate
    ConfigurationController.ValidateServiceHierarchy(hierarchy);

    ActionResult response;
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
    try {
      var (existingEnv, existingTenant, serviceMap) =
        await this.FetchExistingConfiguration(environment, tenant, cancellationToken);

      // Identify services removed, created, updated

      // Construct a Dictionary of service name -> existing service
      var existingServicesByName =
        serviceMap.Values.ToImmutableDictionary(svc => svc.Name, StringComparer.OrdinalIgnoreCase);
      // Enumerate services in hierarchy.Services, partition into an toAdd list and a toUpdate list
      var partitionedServices =
        hierarchy.Services
          .ToLookup(svc => existingServicesByName.ContainsKey(svc.Name));

      var servicesToAdd = partitionedServices[false].ToImmutableList();
      var servicesToUpdate = partitionedServices[true].ToImmutableList();

      // Create a list of existing services that need to be deleted.
      var newServicesByName =
        hierarchy.Services.ToImmutableDictionary(svc => svc.Name, StringComparer.OrdinalIgnoreCase);

      var servicesToDelete = serviceMap.Values.Where(svc => !newServicesByName.ContainsKey(svc.Name)).ToImmutableList();

      // Identify relationships removed, created, updated

      // build the map of name => set of names for the new configuration
      var newRelationshipLookup =
        hierarchy.Services.ToImmutableDictionary(
          svc => svc.Name,
          svc => svc.Children?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase) ?? ImmutableHashSet<String>.Empty,
          StringComparer.OrdinalIgnoreCase);

      // Fetch the existing relationships
      var serviceRelationships =
        await this.FetchExistingRelationships(serviceMap.Keys, cancellationToken);

      // build the map of name => set of names for the existing configuration
      var existingRelationships =
        serviceRelationships.ToLookup(
          rel => serviceMap[rel.ParentServiceId].Name,
          rel => serviceMap[rel.ServiceId].Name,
          StringComparer.OrdinalIgnoreCase);

      var relationshipsToAdd = new List<(String parentName, String childName)>();
      var relationshipsToRemove = new List<(String parentName, String childName)>();
      // for each key name in existing relationship map:
      //   get the set of relationships in the new relationships map
      //   the relationships to remove = set difference existing children - new children
      //   the relationships to add = set difference new children - existing
      foreach (var rel in existingRelationships) {
        if (newRelationshipLookup.TryGetValue(rel.Key, out var newChildren)) {
          // The service still exists in the new config
          // Compare the list of children to see if any need to be removed or added

          // Convert rel (the IGrouping of children names) to a HashSet
          var existingChildren = rel.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

          // Use set.Except to determine the differences
          // Get relationships to remove
          var removeDiff = existingChildren.Except(newChildren);

          relationshipsToRemove.AddRange(
            removeDiff.Select(
              child => (rel.Key, child)
            )
          );

          // Get relationships to add
          var addDiff = newChildren.Except(existingChildren);

          relationshipsToAdd.AddRange(
            addDiff.Select(
              child => (rel.Key, child)
            )
          );
        } else {
          // Add all relationships in rel to the toRemove list
          relationshipsToRemove.AddRange(rel.Select(child => (rel.Key, child)));
        }
      }

      // for each key name in new relationship map
      //   if the key does not exist in existing relationships map
      //   then add that to the relationships to add
      foreach (var rel in newRelationshipLookup) {
        if (!existingRelationships.Contains(rel.Key)) {
          relationshipsToAdd.AddRange(rel.Value.Select(child => (rel.Key, child)));
        }
      }

      // Insert servicesToAdd services into the DB

      var createdServices = await this._servicesTable.AddAllAsync(
        servicesToAdd.Select(svcConfig => Service.New(
          existingTenant.Id,
          svcConfig.Name,
          svcConfig.DisplayName,
          svcConfig.Description,
          svcConfig.Url,
          hierarchy.RootServices.Contains(svcConfig.Name, StringComparer.OrdinalIgnoreCase)
        )),
        cancellationToken
      );

      // Update servicesToUpdate
      this._servicesTable.UpdateRange(
        servicesToUpdate.Select(svcConfig => {
          var existing = existingServicesByName[svcConfig.Name];
          return new Service(
            existing.Id,
            existing.TenantId,
            svcConfig.Name,
            svcConfig.DisplayName,
            svcConfig.Description,
            svcConfig.Url,
            hierarchy.RootServices.Contains(svcConfig.Name, StringComparer.OrdinalIgnoreCase)
          );
        })
      );

      // Update existingServicesByName to include the created services.
      existingServicesByName =
        existingServicesByName.SetItems(createdServices.ToImmutableDictionary(svc => svc.Name));

      // Delete relationshipsToRemove
      this._relationshipsTable.RemoveRange(
        relationshipsToRemove.Select(rel => new ServiceRelationship(
          existingServicesByName[rel.parentName].Id,
          existingServicesByName[rel.childName].Id
        ))
      );

      // Add relationshipsToAdd
      await this._relationshipsTable.AddAllAsync(
        relationshipsToAdd.Select(rel => new ServiceRelationship(
          existingServicesByName[rel.parentName].Id,
          existingServicesByName[rel.childName].Id
        )),
        cancellationToken
      );

      // Delete servicesToDelete
      this._servicesTable.RemoveRange(servicesToDelete);

      await this._dbContext.SaveChangesAsync(cancellationToken);

      // re-read everything after updates.
      var (_, _, updatedServiceMap) =
        await this.FetchExistingConfiguration(environment, tenant, cancellationToken);
      var updatedRelationships =
        await this.FetchExistingRelationships(updatedServiceMap.Keys, cancellationToken);

      response = this.Ok(ConfigurationController.CreateServiceHierarchy(
        updatedServiceMap,
        updatedRelationships.ToLookup(rel => rel.ParentServiceId)
      ));

      await tx.CommitAsync(cancellationToken);
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    return response;
  }

  private static void ValidateServiceHierarchy(ServiceHierarchyConfiguration hierarchy) {
    var serviceNames =
      hierarchy.Services.Select(svc => svc.Name).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    var missingRootServices =
      hierarchy.RootServices
        .Where(rootSvcName => !serviceNames.Contains(rootSvcName))
        .ToImmutableList();
    if (missingRootServices.Any()) {
      throw new BadRequestException(
        message: "One or more of the specified root services do not exist in the services array.",
        new Dictionary<String, Object?> {
          { nameof(ServiceHierarchyConfiguration.RootServices), missingRootServices }
        }
      );
    }

    var missingChildServices =
      hierarchy.Services
        .Select(svc =>
          new {
            Name = svc.Name,
            Children = svc.Children?.Where(child => !serviceNames.Contains(child)).ToImmutableList()
          })
        .Where(v => v.Children?.Any() == true)
        .ToImmutableList();

    if (missingChildServices.Any()) {
      throw new BadRequestException(
        message:
        "One or more of the specified services contained a reference to a child service that did not exist in the services array.",
        new Dictionary<String, Object?> {
          { nameof(ServiceHierarchyConfiguration.Services), missingChildServices }
        }
      );
    }
  }

  private async Task<(Environment, Tenant, ImmutableDictionary<Guid, Service>)> FetchExistingConfiguration(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    var results =
      await this._environmentsTable
        .Where(e => e.Name == environmentName)
        .LeftJoin(
          this._tenantsTable.Where(t => t.Name == tenantName),
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
        .ToListAsync(cancellationToken);

    var environment = results.FirstOrDefault()?.Environment;
    var tenant = results.FirstOrDefault()?.Tenant;
    if (environment == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    }

    var serviceMap =
      results
        .Select(r => r.Service)
        .NotNull()
        .ToImmutableDictionary(svc => svc.Id);

    return (environment, tenant, serviceMap);
  }

  private async Task<IList<ServiceRelationship>> FetchExistingRelationships(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return
      await this._relationshipsTable
        .Where(r => serviceIds.Contains(r.ParentServiceId))
        .ToListAsync(cancellationToken);
  }

  private static ServiceHierarchyConfiguration CreateServiceHierarchy(
    IImmutableDictionary<Guid, Service> serviceMap,
    ILookup<Guid, ServiceRelationship> serviceRelationshipsByParent) {
    return new ServiceHierarchyConfiguration(
      serviceMap.Values
        .Select(svc => new ServiceConfiguration(
          svc.Name,
          svc.DisplayName,
          svc.Description,
          svc.Url,
          serviceRelationshipsByParent[svc.Id]
            .Select(id => serviceMap[id.ServiceId].Name)
            .NullIfEmpty()
            ?.ToImmutableHashSet()
        ))
        .ToImmutableList(),
      serviceMap.Values
        .Where(svc => svc.IsRootService)
        .Select(svc => svc.Name)
        .ToImmutableHashSet()
    );
  }
}
