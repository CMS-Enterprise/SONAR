using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Helpers;

public class TenantDataHelper {
  public const String SonarTenantName = "sonar";

  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly HealthDataHelper _healthDataHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly VersionDataHelper _versionDataHelper;
  private readonly String _sonarEnvironment;
  private readonly TagsDataHelper _tagsDataHelper;
  private readonly DataContext _dbContext;

  public TenantDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    HealthDataHelper healthDataHelper,
    ServiceDataHelper serviceDataHelper,
    VersionDataHelper versionDataHelper,
    IOptions<SonarHealthCheckConfiguration> sonarHealthConfig,
    TagsDataHelper tagsDataHelper,
    DataContext dbContext) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._healthDataHelper = healthDataHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._versionDataHelper = versionDataHelper;
    this._tagsDataHelper = tagsDataHelper;
    this._sonarEnvironment = sonarHealthConfig.Value.SonarEnvironment;
    this._dbContext = dbContext;
  }

  public async Task<Tenant> FetchOrCreateTenantAsync(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    var (environment, tenant) = await this.TryFetchTenantAsync(environmentName, tenantName, cancellationToken);

    if (environment is null) {
      throw new ResourceNotFoundException(nameof(environment), environmentName);
    }

    if (tenant is null) {
      var newTenant = await this._tenantsTable.AddAsync(Tenant.New(environment.Id, tenantName), cancellationToken);
      await this._dbContext.SaveChangesAsync(cancellationToken);
      tenant = newTenant.Entity;
    }

    return tenant;
  }

  public async Task<Tenant> FetchExistingTenantAsync(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    // Check if the environment and tenant exist
    var (environment, tenant) = await this.TryFetchTenantAsync(environmentName, tenantName, cancellationToken);

    if (environment == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    }

    return tenant;
  }

  public async Task<(Environment?, Tenant?)> TryFetchTenantAsync(
    String environmentName,
    String tenantName,
    CancellationToken cancellationToken) {

    var result = await this._environmentsTable
      .Where(e => e.Name == environmentName)
      .LeftJoin(
        this._tenantsTable.Where(t => t.Name == tenantName),
        leftKeySelector: e => e.Id,
        rightKeySelector: t => t.EnvironmentId,
        resultSelector: (env, t) => new { Environment = env, Tenant = t })
      .SingleOrDefaultAsync(cancellationToken);

    return (result?.Environment, result?.Tenant);
  }

  public Task<IList<Tenant>> ListTenantsForEnvironment(
    Guid environmentId,
    CancellationToken cancellationToken) {

    return this._tenantsTable.Where(t => t.EnvironmentId == environmentId)
      .ToListAsync(cancellationToken)
      .ContinueWith(list => (IList<Tenant>)list.Result, cancellationToken);
  }

  public async Task<Dictionary<String, List<String>>> GetEnvironmentTenantTree(
    CancellationToken cancellationToken) {

    var environments = await this._environmentsTable
      .ToListAsync(cancellationToken);
    var envTenantTree = new Dictionary<String, List<String>>();
    foreach (var env in environments) {
      var tenants = await this.ListTenantsForEnvironment(env.Id, cancellationToken);
      envTenantTree.Add(env.Name, tenants.Select(tenant => tenant.Name).ToList());
    }

    return envTenantTree;
  }

  public async Task<IList<TenantInfo>> GetTenantsInfo(
    Environment environment,
    CancellationToken cancellationToken) {

    var tenants = await
      this._tenantsTable.Where(t => t.EnvironmentId == environment.Id)
        .ToListAsync(cancellationToken);

    var tenantList = new List<TenantInfo>();
    foreach (var tenant in tenants) {
      var (_, existingTenant, services) =
        await this._serviceDataHelper.FetchExistingConfiguration(environment.Name, tenant.Name, cancellationToken);

      var serviceStatuses = await this._healthDataHelper.GetServiceStatuses(
        environment.Name, tenant.Name, cancellationToken);

      var healthCheckStatus = await this._healthDataHelper.GetHealthCheckStatus(
        environment.Name, tenant.Name, cancellationToken);

      var serviceChildIdsLookup = await this._serviceDataHelper.GetServiceChildIdsLookup(services, cancellationToken);
      var healthChecksByService = await this._healthDataHelper.GetHealthChecksByService(services, cancellationToken);
      var serviceVersionLookup =
        await this._versionDataHelper.GetServiceVersionLookup(environment.Name, tenant.Name, cancellationToken);
      var tagsByService = await this._tagsDataHelper.FetchExistingServiceTags(services.Keys, cancellationToken);
      var tenantTags = await this._tagsDataHelper.FetchExistingTenantTags(existingTenant.Id, cancellationToken);
      //All root services for tenant
      var rootServiceHealth = services.Values.Where(svc => svc.IsRootService)
        .Select(svc => this._healthDataHelper.ToServiceHealth(
          svc,
          services,
          serviceStatuses,
          serviceChildIdsLookup,
          healthChecksByService,
          healthCheckStatus,
          tagsByService.ToLookup(st => st.ServiceId),
          this._tagsDataHelper.GetResolvedTenantTags(tenantTags.ToList()),
          environment.Name,
          tenant.Name,
          ImmutableQueue.Create<String>(svc.Name))
        ).ToArray();

      tenantList.Add(ToTenantInfo(tenant.Name, environment, serviceVersionLookup, rootServiceHealth));
    }

    if (environment.Name == this._sonarEnvironment) {
      tenantList.Add(ToTenantInfo(
        SonarTenantName,
        environment,
        // Note: we could fetch version information for our built in dependencies
        ImmutableDictionary<String, IImmutableList<ServiceVersionDetails>>.Empty,
        (await this._healthDataHelper.CheckSonarHealth(cancellationToken)).ToArray()
      ));
    }

    return tenantList;
  }

  private static TenantInfo ToTenantInfo(
    String tenantName,
    Environment environment,
    IImmutableDictionary<String, IImmutableList<ServiceVersionDetails>> serviceVersionLookup,
    ServiceHierarchyHealth[] rootServiceHealth
  ) {
    HealthStatus? aggregateStatus = HealthStatus.Unknown;
    DateTime? statusTimestamp = null;

    foreach (var rs in rootServiceHealth) {
      if (rs.AggregateStatus.HasValue) {
        if ((aggregateStatus == null) ||
          (aggregateStatus < rs.AggregateStatus) ||
          (rs.AggregateStatus == HealthStatus.Unknown)) {
          aggregateStatus = rs.AggregateStatus;
        }

        // The child service should always have a timestamp here, but double check anyway
        if (rs.Timestamp.HasValue &&
          (!statusTimestamp.HasValue || (rs.Timestamp.Value < statusTimestamp.Value))) {
          // The status timestamp should always be the *oldest* of the
          // recorded status data points.
          statusTimestamp = rs.Timestamp.Value;
        }
      } else {
        // One of the child services has an "unknown" status, that means
        // this service will also have the "unknown" status.
        aggregateStatus = null;
        statusTimestamp = null;
        break;
      }
    }

    return new TenantInfo(
      environment.Name,
      tenantName,
      environment.IsNonProd,
      statusTimestamp,
      aggregateStatus,
      AddVersionInfo(rootServiceHealth, serviceVersionLookup).ToArray()
    );
  }

  private static IEnumerable<ServiceHierarchyInfo> AddVersionInfo(
    IEnumerable<ServiceHierarchyHealth> serviceHealth,
    IImmutableDictionary<String, IImmutableList<ServiceVersionDetails>> serviceVersionLookup) {
    foreach (var svc in serviceHealth) {
      yield return new ServiceHierarchyInfo(
        svc.Name,
        svc.DisplayName,
        svc.DashboardLink,
        svc.Description,
        svc.Url,
        svc.Timestamp,
        svc.AggregateStatus,
        serviceVersionLookup.TryGetValue(svc.Name, out var versionDetailsList) ?
          versionDetailsList.ToImmutableDictionary(v => v.VersionType, v => v.Version) : null,
        svc.HealthChecks,
        svc.Children != null ? AddVersionInfo(svc.Children, serviceVersionLookup).ToImmutableHashSet() : null,
        svc.Tags
      );
    }
  }
}
