using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Alerting;
using Cms.BatCave.Sonar.Alertmanager;
using Cms.BatCave.Sonar.Authentication;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/config")]
public class ConfigurationController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DbSet<ServiceRelationship> _relationshipsTable;
  private readonly DbSet<HealthCheck> _healthsTable;
  private readonly DbSet<VersionCheck> _versionChecksTable;
  private readonly DbSet<ServiceTag> _serviceTagsTable;
  private readonly DbSet<TenantTag> _tenantTagsTable;
  private readonly DbSet<AlertingConfigurationVersion> _alertingConfigurationVersionTable;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly String _sonarEnvironment;
  private readonly TagsDataHelper _tagsDataHelper;
  private readonly AlertingDataHelper _alertingDataHelper;
  private readonly AlertingConfigurationManager _alertmanagerHelper;
  private readonly IOptions<KubernetesApiAccessConfiguration> _kubernetesApiAccessConfig;

  public ConfigurationController(
    DataContext dbContext,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<ServiceRelationship> relationshipsTable,
    DbSet<HealthCheck> healthsTable,
    DbSet<VersionCheck> versionChecksTable,
    ServiceDataHelper serviceDataHelper,
    ApiKeyDataHelper apiKeyDataHelper,
    TenantDataHelper tenantDataHelper,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig,
    TagsDataHelper tagsDataHelper,
    DbSet<ServiceTag> serviceTagsTable,
    DbSet<TenantTag> tenantTagsTable,
    AlertingDataHelper alertingDataHelper,
    DbSet<AlertingConfigurationVersion> alertingConfigurationVersionTable,
    AlertingConfigurationManager alertmanagerHelper,
    IOptions<KubernetesApiAccessConfiguration> kubernetesApiAccessConfig) {
    this._dbContext = dbContext;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._relationshipsTable = relationshipsTable;
    this._healthsTable = healthsTable;
    this._versionChecksTable = versionChecksTable;
    this._serviceDataHelper = serviceDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._tagsDataHelper = tagsDataHelper;
    this._serviceTagsTable = serviceTagsTable;
    this._tenantTagsTable = tenantTagsTable;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
    this._alertingDataHelper = alertingDataHelper;
    this._alertingConfigurationVersionTable = alertingConfigurationVersionTable;
    this._alertmanagerHelper = alertmanagerHelper;
    this._kubernetesApiAccessConfig = kubernetesApiAccessConfig;
  }

  /// <summary>
  ///   Retrieves the configuration for the specified environment and tenant.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The tenant configuration was found and will be returned.</response>
  /// <response code="404">The specified environment or tenant was not found.</response>
  [HttpGet("{environment}/tenants/{tenant}", Name = "GetTenant")]
  [ProducesResponseType(typeof(ServiceHierarchyConfiguration), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [AllowAnonymous]
  public async Task<ActionResult> GetConfiguration(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    CancellationToken cancellationToken = default) {

    if (String.Equals(environment, this._sonarEnvironment, StringComparison.OrdinalIgnoreCase) &&
      String.Equals(tenant, TenantDataHelper.SonarTenantName, StringComparison.OrdinalIgnoreCase)) {
      var sonarConfiguration = this._serviceDataHelper.FetchSonarConfiguration();
      return this.Ok(sonarConfiguration);
    }



    var (envEntity, tenantEntity, serviceMap) =
      await this._serviceDataHelper.FetchExistingConfiguration(
        environment,
        tenant,
        cancellationToken
      );

    var isAuthorized =
      this.User.HasTenantAccess(envEntity.Id, tenantEntity.Id, PermissionType.Admin);

    var serviceRelationshipsByParent =
      (await this._serviceDataHelper.FetchExistingRelationships(serviceMap.Keys, cancellationToken))
      .ToLookup(r => r.ParentServiceId);

    var healthCheckByService =
      (await this._serviceDataHelper.FetchExistingHealthChecks(serviceMap.Keys, cancellationToken))
      .ToLookup(hc => hc.ServiceId);

    var versionCheckByService =
      (await this._serviceDataHelper.FetchExistingVersionChecks(serviceMap.Keys, cancellationToken))
      .ToLookup(vc => vc.ServiceId);

    var tenantTags =
      (await this._tagsDataHelper.FetchExistingTenantTags(tenantEntity.Id, cancellationToken))
      .NullIfEmpty()?
      .ToImmutableDictionary(t => t.Name, t => t.Value);

    var serviceTags =
      (await this._tagsDataHelper.FetchExistingServiceTags(serviceMap.Keys, cancellationToken))
      .ToLookup(s => s.ServiceId);

    var alertReceiversById =
      (await this._alertingDataHelper.FetchAlertReceiversAsync(tenantEntity.Id, cancellationToken))
      .ToImmutableDictionary(keySelector: r => r.Id);

    var alertingRulesByServiceId =
      (await this._alertingDataHelper.FetchAlertingRulesAsync(serviceMap.Keys, cancellationToken))
      .ToLookup(keySelector: r => r.ServiceId);

    var serviceHierarchy = CreateServiceHierarchy(
      serviceMap,
      healthCheckByService,
      versionCheckByService,
      serviceRelationshipsByParent,
      tenantTags,
      serviceTags,
      alertReceiversById,
      alertingRulesByServiceId);

    if (!isAuthorized) {
      serviceHierarchy = ServiceDataHelper.Redact(serviceHierarchy);
    }

    return this.Ok(serviceHierarchy);
  }

  /// <summary>
  ///   Sets the configuration for a new environment or tenant.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="tenant">The name of the tenant.</param>
  /// <param name="hierarchy">The new service hierarchy configuration.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="201">The tenant configuration successfully created.</response>
  /// <response code="401">The API key in the header is not authorized for creating a new tenant.</response>
  /// <response code="409">
  ///   Configuration for the specified environment and tenant already exists. Use the PUT HTTP Method to
  ///   update this configuration.
  /// </response>
  /// <response code="400">The specified service hierarchy configuration is not valid.</response>
  [HttpPost("{environment}/tenants/{tenant}", Name = "CreateTenant")]
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

      // validate tags
      this._tagsDataHelper.ValidateTenantTags(
        hierarchy.Tags?.Keys ?? Enumerable.Empty<String>(),
        hierarchy.Services.SelectMany(
        s => s.Tags?.Keys ?? Enumerable.Empty<String>()));

      Environment environmentEntity;
      if (result == null) {
        // If the environment does not exist, create it
        var createdEnvironment = await this._environmentsTable.AddAsync(
          new Environment(
            Guid.Empty,
            environment,
            false),
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

      // create tenant tags (if present)
      var createdTenantTags = Enumerable.Empty<TenantTag>();
      if (hierarchy.Tags != null) {
        createdTenantTags =
          await this._tenantTagsTable.AddAllAsync(
            hierarchy.Tags.Select(
              tag => TenantTag.New(
                createdTenant.Entity.Id,
                tag.Key,
                tag.Value)
            ),
        cancellationToken
          );
      }

      // create service tags (if present)
      var createdServiceTags = Enumerable.Empty<ServiceTag>();
      foreach (var service in hierarchy.Services) {
        if (service.Tags != null && service.Tags.Count > 0) {
          createdServiceTags =
            await this._serviceTagsTable.AddAllAsync(
              service.Tags.Select(
                tag => ServiceTag.New(
                  createdServices.First(svc => service.Name == svc.Name).Id,
                  tag.Key,
                  tag.Value)
              ),
              cancellationToken
            );
        }
      }

      var servicesByName =
        createdServices.ToImmutableDictionary(keySelector: svc => svc.Name);

      var createdHealthChecks = await this._healthsTable.AddAllAsync(
        hierarchy.Services.SelectMany(svc =>
          svc.HealthChecks != null ?
            svc.HealthChecks.Select(hc => HealthCheck.New(
              servicesByName[svc.Name].Id,
              hc.Name,
              hc.Description ?? String.Empty,
              hc.Type,
              HealthCheck.SerializeDefinition(hc.Type, hc.Definition),
              hc.SmoothingTolerance ?? 0
            )) :
            Enumerable.Empty<HealthCheck>()
        ),
        cancellationToken
      );

      var createdVersionChecks = await this._versionChecksTable.AddAllAsync(
        hierarchy.Services.SelectMany(svc =>
          svc.VersionChecks != null ?
            svc.VersionChecks.Select(vc => VersionCheck.New(
              servicesByName[svc.Name].Id,
              vc.VersionCheckType,
              VersionCheck.SerializeDefinition(vc.VersionCheckType, vc.Definition)
            )) :
            Enumerable.Empty<VersionCheck>()
        ),
        cancellationToken
      );

      // Create the service_relationships based on children lists
      var createdRelationships = await this._relationshipsTable.AddAllAsync(
        hierarchy.Services.SelectMany(svc =>
          svc.Children != null ?
            svc.Children.Select(child => new ServiceRelationship(
              servicesByName[svc.Name].Id,
              servicesByName[child].Id
            )) :
            Enumerable.Empty<ServiceRelationship>()
        ),
        cancellationToken
      );

      // Add tenant alerting configuration and service alerting rules if any
      // are defined on the incoming hierarchy configuration object.
      IImmutableList<AlertReceiver> createdAlertReceivers = ImmutableList<AlertReceiver>.Empty;
      IImmutableList<AlertingRule> createdAlertingRules = ImmutableList<AlertingRule>.Empty;
      var alertReceiversChanged = false;
      var alertingRulesChanged = false;

      if (hierarchy.Alerting is not null) {
        (alertReceiversChanged, createdAlertReceivers) =
          await this._alertingDataHelper.CreateOrUpdateAlertReceiversAsync(
            tenantId: createdTenant.Entity.Id,
            alertReceiverConfigurations: hierarchy.Alerting.Receivers,
            cancellationToken);

        var accumulatedAlertingRules = new List<AlertingRule>();

        foreach (var svc in hierarchy.Services) {
          if (svc.AlertingRules is not null) {
            var (_, alertingRules) = await this._alertingDataHelper.CreateOrUpdateAlertingRulesAsync(
              serviceId: servicesByName[svc.Name].Id,
              alertReceivers: createdAlertReceivers,
              alertingRuleConfigurations: svc.AlertingRules,
              cancellationToken);
            accumulatedAlertingRules.AddRange(alertingRules);
          }
        }

        createdAlertingRules = accumulatedAlertingRules.ToImmutableList();
        if (createdAlertingRules.Any()) {
          alertingRulesChanged = true;
        }
      }

      // Increment Alerting Configuration Version
      if (alertReceiversChanged || alertingRulesChanged) {
        await this._alertingConfigurationVersionTable.AddAsync(new AlertingConfigurationVersion(DateTime.UtcNow),
          cancellationToken);
      }

      await this._dbContext.SaveChangesAsync(cancellationToken);

      response = this.Created(
        this.Url.Action(nameof(this.GetConfiguration), new { environment, tenant }) ?? String.Empty,
        CreateServiceHierarchy(
          servicesByName.Values.ToImmutableDictionary(svc => svc.Id),
          createdHealthChecks.ToLookup(hc => hc.ServiceId),
          createdVersionChecks.ToLookup(vc => vc.ServiceId),
          createdRelationships.ToLookup(rel => rel.ParentServiceId),
          createdTenantTags.NullIfEmpty()?.ToImmutableDictionary(tt => tt.Name, tt => tt.Value),
          createdServiceTags.ToLookup(st => st.ServiceId),
          createdAlertReceivers.ToImmutableDictionary(keySelector: r => r.Id),
          createdAlertingRules.ToLookup(keySelector: r => r.ServiceId)
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
  /// <response code="401">The API key in the header is not authorized for updating a tenant.</response>
  /// <response code="404">The specified environment or tenant was not found.</response>
  /// <response code="400">The specified service hierarchy configuration is not valid.</response>
  [HttpPut("{environment}/tenants/{tenant}", Name = "UpdateTenant")]
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

    ActionResult response;

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
    try {
      var (_, existingTenant, serviceMap) =
        await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);

      // validate tags
      this._tagsDataHelper.ValidateTenantTags(
        hierarchy.Tags?.Keys ?? Enumerable.Empty<String>(),
        hierarchy.Services.SelectMany(
          s => s.Tags?.Keys ?? Enumerable.Empty<String>()));

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
        await this._serviceDataHelper.FetchExistingRelationships(serviceMap.Keys, cancellationToken);

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

      // Tags //
      var tenantTagsToAdd = new Dictionary<String, String?>();
      var tenantTagsToRemove = new List<TenantTag>();
      var tenantTagsToUpdate = new List<TenantTag>();

      var existingTenantTags =
        await this._tagsDataHelper.FetchExistingTenantTags(existingTenant.Id, cancellationToken);

      if (hierarchy.Tags == null) {
        // new hierarchy has no tags, remove all existing tags
        tenantTagsToRemove.AddRange(existingTenantTags);
      } else {
        // validate tags

        // all current tags that are not in existing tags are tags to add
        foreach (var newTag in hierarchy.Tags) {
          if (!existingTenantTags.Any(et => et.Name == newTag.Key)) {
            tenantTagsToAdd.Add(newTag.Key, newTag.Value);
          }
        }

        // determine tags to update and tags to remove
        foreach (var existingTag in existingTenantTags) {
          if (hierarchy.Tags.TryGetValue(existingTag.Name, out var newValue)) {
            // tag exists, determine if value update is needed
            if (newValue != existingTag.Value) {
              tenantTagsToUpdate.Add(new TenantTag(
                existingTag.Id,
                existingTag.TenantId,
                existingTag.Name,
                newValue)
              );
            }
          } else {
            // tag does not exist, remove old
            tenantTagsToRemove.Add(existingTag);
          }
        }
      }

      // Tags //
      var serviceTagsToAdd = new List<(String serviceName, String tagName, String? tagValue)>();
      var serviceTagsToRemove = new List<ServiceTag>();
      var serviceTagsToUpdate = new List<ServiceTag>();

      var existingServiceTags = await this._tagsDataHelper.FetchExistingServiceTags(serviceMap.Keys, cancellationToken);
      var existingServiceTagLookup = existingServiceTags.ToLookup(
        st => serviceMap[st.ServiceId].Name,
        StringComparer.OrdinalIgnoreCase);

      var newServiceTags =
        hierarchy.Services.ToImmutableDictionary(
          svc => svc.Name,
          svc => svc.Tags?.ToImmutableList() ??
            ImmutableList<KeyValuePair<String, String?>>.Empty);

      // tags to add
      foreach (var newTagsByService in newServiceTags) {
        var serviceName = newTagsByService.Key;
        if (!existingServiceTagLookup.Contains(serviceName)) {
          serviceTagsToAdd.AddRange(newTagsByService.Value
            .Select(kvp => (serviceName, kvp.Key, kvp.Value)));
        } else {
          foreach (var tag in newTagsByService.Value) {
            if (!existingServiceTagLookup[serviceName].Any(existingTag => existingTag.Name == tag.Key)) {
              serviceTagsToAdd.Add((serviceName, tag.Key, tag.Value));
            }
          }
        }
      }

      // iterate through each service and determine tags to update and tags to remove
      foreach (var existingServiceTagGrouping in existingServiceTagLookup) {
        if (newServiceTags.TryGetValue(existingServiceTagGrouping.Key, out var newTags)) {
          var newTagsDictionary = newTags.ToImmutableDictionary(
            nt => nt.Key,
            nt => nt.Value);

          foreach (var existingTag in existingServiceTagGrouping) {
            if (newTagsDictionary.TryGetValue(existingTag.Name, out var newValue)) {
              // tag exists, determine if value update is needed
              if (newValue != existingTag.Value) {
                serviceTagsToUpdate.Add(new ServiceTag(
                  existingTag.Id,
                  existingTag.ServiceId,
                  existingTag.Name,
                  newValue));
              }
            } else {
              // tag does not exist, remove old
              serviceTagsToRemove.Add(existingTag);
            }
          }
        } else {
          // service exists but no tags
          serviceTagsToRemove.AddRange(existingServiceTagGrouping);
        }
      }

      var healthChecksToAdd = new List<(String serviceName, HealthCheckModel newHealthCheck)>();
      var healthChecksToRemove = new List<HealthCheck>();
      var healthChecksToUpdate = new List<(HealthCheck existingHealthCheck, HealthCheckModel updatedHealthCheck)>();

      // Transform new services collection into a Dictionary to be able to search by name of services for list of health checks.
      var newHealthCheck =
        hierarchy.Services.ToImmutableDictionary(
          svc => svc.Name,
          svc => svc.HealthChecks?.ToImmutableList() ?? ImmutableList<HealthCheckModel>.Empty);

      // Fetch all of existing health check and
      var existingHealthChecks =
        await this._serviceDataHelper.FetchExistingHealthChecks(serviceMap.Keys, cancellationToken);

      // Allow for lookup by name
      var existingHealthCheckLookup =
        existingHealthChecks.ToLookup(
          hc => serviceMap[hc.ServiceId].Name,
          StringComparer.OrdinalIgnoreCase);

      // foreach the existing relationships
      foreach (var serviceHealthChecks in existingHealthCheckLookup) {
        // Search by Key (Service Name)
        if (newHealthCheck.TryGetValue(serviceHealthChecks.Key, out var newHealthChecks)) {
          // Handle existing relationships, for a service that no longer exists (add to health checks to remove)
          var existingChecks = serviceHealthChecks.ToImmutableList();
          foreach (var existingCheck in existingChecks) {
            //Search in the health check model where the names match the health check data.
            var newCheck = newHealthChecks.Find(
              newhc => (newhc.Name.Equals(existingCheck.Name, StringComparison.OrdinalIgnoreCase)));
            if (newCheck != null) {
              // Existing healthcheck has new service present payload and existing service healthcheck (update)
              healthChecksToUpdate.Add((existingCheck, newCheck));
            } else {
              healthChecksToRemove.Add(existingCheck);
            }
          }
        } else {
          // Service exists but does not have a health check (remove)
          healthChecksToRemove.AddRange(serviceHealthChecks);
        }
      }

      // for each health check in payload, if does not exist in existing lookup then add
      foreach (var newHealthChecks in newHealthCheck) {
        if (!existingHealthCheckLookup.Contains(newHealthChecks.Key)) {
          healthChecksToAdd.AddRange(newHealthChecks.Value.Select(hcm => (newHealthChecks.Key, hcm)));
        } else {
          foreach (var check in newHealthChecks.Value) {
            if (!existingHealthCheckLookup[newHealthChecks.Key].Any(existingCheck => existingCheck.Name.Equals(check.Name, StringComparison.OrdinalIgnoreCase))) {
              healthChecksToAdd.Add((newHealthChecks.Key, check));
            }
          }
        }
      }

      // update version checks
      var versionChecksToAdd = new List<(String serviceName, VersionCheckModel newVersionCheck)>();
      var versionChecksToRemove = new List<VersionCheck>();
      var versionChecksToUpdate =
        new List<(VersionCheck existingVersionCheck, VersionCheckModel updatedVersionCheck)>();

      // Transform new services collection into a Dictionary to be able to search by name of services for list of version checks.
      var newVersionCheck =
        hierarchy.Services.ToImmutableDictionary(
          svc => svc.Name,
          svc => svc.VersionChecks?.ToImmutableList() ?? ImmutableList<VersionCheckModel>.Empty);

      // Fetch all of existing verion checks and
      var existingVersionChecks =
        await this._serviceDataHelper.FetchExistingVersionChecks(serviceMap.Keys, cancellationToken);

      // Allow for lookup by name
      var existingVersionCheckLookup =
        existingVersionChecks.ToLookup(
          hc => serviceMap[hc.ServiceId].Name,
          StringComparer.OrdinalIgnoreCase);

      foreach (var serviceVersionChecks in existingVersionCheckLookup) {
        if (newVersionCheck.TryGetValue(serviceVersionChecks.Key, out var newVersionChecks)) {
          var existingChecks = serviceVersionChecks.ToImmutableList();
          foreach (var existingCheck in existingChecks) {
            var newCheck = newVersionChecks.Find(
              newVc =>
                (newVc.VersionCheckType.ToString().Equals(existingCheck.VersionCheckType.ToString(), StringComparison.OrdinalIgnoreCase)));
            if (newCheck != null) {
              versionChecksToUpdate.Add((existingCheck, newCheck));
            } else {
              versionChecksToRemove.Add(existingCheck);
            }
          }
        } else {
          versionChecksToRemove.AddRange(serviceVersionChecks);
        }
      }

      // for each version check in payload, if does not existing in existing lookup, then add
      foreach (var newVersionChecks in newVersionCheck) {
        if (!existingVersionCheckLookup.Contains(newVersionChecks.Key)) {
          versionChecksToAdd.AddRange(newVersionChecks.Value.Select(vcm => (newVersionChecks.Key, vcm)));
        } else {
          foreach (var check in newVersionChecks.Value) {
            if (!existingVersionCheckLookup[newVersionChecks.Key].Any(
              existingCheck => existingCheck.VersionCheckType
                .ToString()
                .Equals(check.VersionCheckType.ToString(), StringComparison.OrdinalIgnoreCase))
            ) {
              versionChecksToAdd.Add((newVersionChecks.Key, check));
            }
          }
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

      // apply changes to tags table
      // remove tags
      this._tenantTagsTable.RemoveRange(tenantTagsToRemove);
      this._serviceTagsTable.RemoveRange(serviceTagsToRemove);

      // add new tags
      await this._tenantTagsTable.AddAllAsync(
        tenantTagsToAdd.Select(tt => TenantTag.New(
          existingTenant.Id,
          tt.Key,
          tt.Value)),
        cancellationToken);

      await this._serviceTagsTable.AddAllAsync(
        serviceTagsToAdd.Select(st => ServiceTag.New(
          existingServicesByName[st.serviceName].Id,
          st.tagName,
          st.tagValue)),
        cancellationToken);

      // update tags
      this._tenantTagsTable.UpdateRange(
        tenantTagsToUpdate.Select(tt => new TenantTag(
          tt.Id,
          tt.TenantId,
          tt.Name,
          tt.Value)));

      this._serviceTagsTable.UpdateRange(
        serviceTagsToUpdate.Select(st => new ServiceTag(
          st.Id,
          st.ServiceId,
          st.Name,
          st.Value)));

      //Apply changes to health checks to tables.
      this._healthsTable.RemoveRange(healthChecksToRemove);

      // Apply changes to version checks table
      this._versionChecksTable.RemoveRange(versionChecksToRemove);

      // Add relationshipsToAdd
      await this._healthsTable.AddAllAsync(
        healthChecksToAdd.Select(healthCheck => HealthCheck.New(
          existingServicesByName[healthCheck.serviceName].Id,
          healthCheck.newHealthCheck.Name,
          healthCheck.newHealthCheck.Description ?? String.Empty,
          healthCheck.newHealthCheck.Type,
          HealthCheck.SerializeDefinition(healthCheck.newHealthCheck.Type, healthCheck.newHealthCheck.Definition),
          healthCheck.newHealthCheck.SmoothingTolerance ?? 0
        )),
        cancellationToken
      );

      // Add version checks
      await this._versionChecksTable.AddAllAsync(
        versionChecksToAdd.Select(versionCheck => VersionCheck.New(
          existingServicesByName[versionCheck.serviceName].Id,
          versionCheck.newVersionCheck.VersionCheckType,
          VersionCheck.SerializeDefinition(versionCheck.newVersionCheck.VersionCheckType,
            versionCheck.newVersionCheck.Definition))),
        cancellationToken
      );

      //Update Table
      this._healthsTable.UpdateRange(
        healthChecksToUpdate.Select(hc => {
          return new HealthCheck(
            hc.existingHealthCheck.Id,
            hc.existingHealthCheck.ServiceId,
            hc.updatedHealthCheck.Name,
            hc.updatedHealthCheck.Description,
            hc.updatedHealthCheck.Type,
            HealthCheck.SerializeDefinition(hc.updatedHealthCheck.Type, hc.updatedHealthCheck.Definition),
            hc.updatedHealthCheck.SmoothingTolerance
          );
        })
      );

      this._versionChecksTable.UpdateRange(
        versionChecksToUpdate.Select(vc => {
          return new VersionCheck(
            vc.existingVersionCheck.Id,
            vc.existingVersionCheck.ServiceId,
            vc.updatedVersionCheck.VersionCheckType,
            VersionCheck.SerializeDefinition(vc.updatedVersionCheck.VersionCheckType,
              vc.updatedVersionCheck.Definition));
        }));

      // Update tenant alerting configuration and service alerting rules if any
      // are defined on the incoming hierarchy configuration object.
      // If the tenant alerting configuration is not present on the incoming
      // hierarchy configuration object, delete it from the DB, which will
      // cascade delete the tenant's service alerting rules from the DB as well.
      IImmutableList<AlertReceiver> updatedAlertReceivers = ImmutableList<AlertReceiver>.Empty;
      IImmutableList<AlertingRule> updatedAlertingRules = ImmutableList<AlertingRule>.Empty;
      var alertReceiversChanged = false;
      var alertingRulesChanged = false;

      if (hierarchy.Alerting is not null) {
        (alertReceiversChanged, updatedAlertReceivers) =
          await this._alertingDataHelper.CreateOrUpdateAlertReceiversAsync(
            tenantId: existingTenant.Id,
            alertReceiverConfigurations: hierarchy.Alerting.Receivers,
            cancellationToken);

        var accumulatedAlertingRules = new List<AlertingRule>();

        foreach (var svc in hierarchy.Services) {
          if (svc.AlertingRules is not null) {
            var (changed, alertingRules) = await this._alertingDataHelper.CreateOrUpdateAlertingRulesAsync(
              serviceId: existingServicesByName[svc.Name].Id,
              alertReceivers: updatedAlertReceivers,
              alertingRuleConfigurations: svc.AlertingRules,
              cancellationToken);
            accumulatedAlertingRules.AddRange(alertingRules);
            if (changed) {
              alertingRulesChanged = true;
            }
          } else {
            if (await this._alertingDataHelper.DeleteAlertingRulesAsync(existingServicesByName[svc.Name].Id)) {
              alertingRulesChanged = true;
            }
          }
        }

        updatedAlertingRules = accumulatedAlertingRules.ToImmutableList();
      } else {
        if (await this._alertingDataHelper.DeleteAlertReceiversAsync(existingTenant.Id)) {
          alertReceiversChanged = true;
        }
      }

      // Increment Alerting Configuration Version
      if (alertReceiversChanged || alertingRulesChanged) {
        await this._alertingConfigurationVersionTable.AddAsync(new AlertingConfigurationVersion(DateTime.UtcNow),
          cancellationToken);
      }

      // Delete servicesToDelete
      this._servicesTable.RemoveRange(servicesToDelete);

      await this._dbContext.SaveChangesAsync(cancellationToken);

      // re-read everything after updates.
      var (_, _, updatedServiceMap) =
        await this._serviceDataHelper.FetchExistingConfiguration(environment, tenant, cancellationToken);
      var updatedHealthChecks =
        await this._serviceDataHelper.FetchExistingHealthChecks(updatedServiceMap.Keys, cancellationToken);
      var updatedVersionChecks =
        await this._serviceDataHelper.FetchExistingVersionChecks(updatedServiceMap.Keys, cancellationToken);
      var updatedRelationships =
        await this._serviceDataHelper.FetchExistingRelationships(updatedServiceMap.Keys, cancellationToken);
      var updatedTenantTags =
        await this._tagsDataHelper.FetchExistingTenantTags(existingTenant.Id, cancellationToken);
      var updatedServiceTags =
        await this._tagsDataHelper.FetchExistingServiceTags(updatedServiceMap.Keys, cancellationToken);

      response = this.Ok(CreateServiceHierarchy(
        updatedServiceMap,
        updatedHealthChecks.ToLookup(hc => hc.ServiceId),
        updatedVersionChecks.ToLookup(vc => vc.ServiceId),
        updatedRelationships.ToLookup(rel => rel.ParentServiceId),
        updatedTenantTags.NullIfEmpty()?.ToImmutableDictionary(k => k.Name, k => k.Value),
        updatedServiceTags.ToLookup(s => s.ServiceId),
        updatedAlertReceivers.ToImmutableDictionary(keySelector: r => r.Id),
        updatedAlertingRules.ToLookup(keySelector: r => r.ServiceId)
      ));

      await tx.CommitAsync(cancellationToken);
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    // TODO: update Alertmanager configuration on a background thread so that
    // it doesn't delay the API response
    // Note: It is important that this happen after SaveChanges successfully
    // completes and the transaction is successfully committed.
    if (this._kubernetesApiAccessConfig.Value.IsEnabled) {
      // Create or update alert recipient configuration
      await this._alertmanagerHelper.CreateOrUpdateAlertmanagerConfigMapAsync(cancellationToken);
    }

    return response;
  }

  [HttpDelete("{environment}/tenants/{tenant}", Name = "DeleteTenant")]
  [ProducesResponseType(statusCode: 204)]
  public async Task<ActionResult> DeleteConfiguration(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    CancellationToken cancellationToken = default) {

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

    try {
      // fetch tenant
      var tenantEntity = await this._tenantDataHelper.FetchExistingTenantAsync(environment, tenant, cancellationToken);

      // Update Configuration Version
      if (await this._alertingDataHelper.DeleteAlertReceiversAsync(tenantEntity.Id)) {
        await this._alertingConfigurationVersionTable.AddAsync(new AlertingConfigurationVersion(DateTime.UtcNow),
          cancellationToken);
      }

      // remove tenant
      this._tenantsTable.Remove(tenantEntity);

      // save
      await this._dbContext.SaveChangesAsync(cancellationToken);
      await tx.CommitAsync(cancellationToken);
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    return this.StatusCode((Int32)HttpStatusCode.NoContent);
  }

  //Converts to Model
  private static ServiceHierarchyConfiguration CreateServiceHierarchy(
    IImmutableDictionary<Guid, Service> serviceMap,
    ILookup<Guid, HealthCheck> healthCheckByService,
    ILookup<Guid, VersionCheck> versionCheckByService,
    ILookup<Guid, ServiceRelationship> serviceRelationshipsByParent,
    IImmutableDictionary<String, String?>? tenantTags,
    ILookup<Guid, ServiceTag>? serviceTagByService,
    IImmutableDictionary<Guid, AlertReceiver> alertReceiversById,
    ILookup<Guid, AlertingRule> alertingRulesByServiceId) {

    AlertingConfiguration? alertingConfiguration = null;
    if (alertReceiversById.Any()) {
      alertingConfiguration = new AlertingConfiguration(
        receivers: alertReceiversById.Values
          .Select(r => new AlertReceiverConfiguration(
            name: r.Name,
            receiverType: r.Type,
            options: r.DeserializeOptions()))
          .ToImmutableList());
    }

    return new ServiceHierarchyConfiguration(
      serviceMap.Values
        .Select(svc => new ServiceConfiguration(
          svc.Name,
          svc.DisplayName,
          svc.Description,
          svc.Url,
          healthCheckByService[svc.Id]
            .Select(check => new HealthCheckModel(
              check.Name,
              check.Description,
              check.Type,
              check.DeserializeDefinition(),
              check.SmoothingTolerance
            ))
            .NullIfEmpty()
            ?.ToImmutableList(),
          versionCheckByService[svc.Id]
            .Select(versionCheck => new VersionCheckModel(
              versionCheck.VersionCheckType,
              versionCheck.DeserializeDefinition()
              ))
            .NullIfEmpty()
            ?.ToImmutableList(),
          serviceRelationshipsByParent[svc.Id]
            .Select(id => serviceMap[id.ServiceId].Name)
            .NullIfEmpty()
            ?.ToImmutableHashSet(),
          serviceTagByService?[svc.Id]
            .ToImmutableDictionary(
              st => st.Name,
              st => st.Value),
          alertingRulesByServiceId[svc.Id].NullIfEmpty()?
            .Select(r => new AlertingRuleConfiguration(
              name: r.Name,
              threshold: r.Threshold,
              receiverName: alertReceiversById[r.AlertReceiverId].Name,
              delay: r.Delay
            ))
            .ToImmutableList()
        ))
        .ToImmutableList(),
      serviceMap.Values
        .Where(svc => svc.IsRootService)
        .Select(svc => svc.Name)
        .ToImmutableHashSet(),
      tenantTags,
      alertingConfiguration
    );
  }
}
