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

public class ServiceDataHelper {
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DbSet<ServiceRelationship> _relationshipsTable;
  private readonly DbSet<HealthCheck> _healthChecksTable;
  private readonly Uri _prometheusUrl;
  private readonly IOptions<DatabaseConfiguration> _dbConfig;

  public ServiceDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<ServiceRelationship> relationshipsTable,
    DbSet<HealthCheck> healthChecksTable,
    IOptions<PrometheusConfiguration> prometheusConfig,
    IOptions<DatabaseConfiguration> dbConfig) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._relationshipsTable = relationshipsTable;
    this._healthChecksTable = healthChecksTable;
    this._prometheusUrl = new Uri(
      $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}/"
    );
    this._dbConfig = dbConfig;
  }

  public async Task<(Environment, Tenant, ImmutableDictionary<Guid, Service>)> FetchExistingConfiguration(
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
          leftKeySelector: row => (row.Tenant != null) ? row.Tenant.Id : (Guid?)null,
          rightKeySelector: svc => svc.TenantId,
          resultSelector: (row, svc) => new {
            row.Environment,
            row.Tenant,
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

  public async Task<Service> FetchExistingService(
    String environmentName,
    String tenantName,
    String serviceName,
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
          this._servicesTable.Where(svc => svc.Name == serviceName),
          leftKeySelector: row => (row.Tenant != null) ? row.Tenant.Id : (Guid?)null,
          rightKeySelector: svc => svc.TenantId,
          resultSelector: (row, svc) => new {
            row.Environment,
            row.Tenant,
            Service = svc
          })
        .ToListAsync(cancellationToken);

    var result = results.SingleOrDefault();
    if (result == null) {
      throw new ResourceNotFoundException(nameof(Environment), environmentName);
    } else if (result.Tenant == null) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    } else if (result.Service == null) {
      throw new ResourceNotFoundException(nameof(Service), serviceName);
    }

    return result.Service;
  }

  public async Task<IList<ServiceRelationship>> FetchExistingRelationships(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return
      await this._relationshipsTable
        .Where(r => serviceIds.Contains(r.ParentServiceId))
        .ToListAsync(cancellationToken);
  }

  public async Task<IList<HealthCheck>> FetchExistingHealthChecks(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return
      await this._healthChecksTable
        .Where(hc => serviceIds.Contains(hc.ServiceId))
        .ToListAsync(cancellationToken);
  }

  public ServiceHierarchyConfiguration FetchSonarConfiguration() {
    // Postgresql Checks
    var postgresDbConnectionTest = new HealthCheckModel(
      name: "connection-test",
      description: "Sonar Database Connection",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Checks for an Invalid Operation Exception",
        expression: null)
      );

    var postgresDbTest = new HealthCheckModel(
      name: "sonar-database-test",
      description: "Database Error",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Checks for an Postgresql Exception",
        expression: null)
      );

    var postgresqlServiceConfig = new ServiceConfiguration(
      name: "postgresql",
      displayName: "Postgresql",
      description: "Postgres Database",
      url: new Uri(
        $"postgresql://{_dbConfig.Value.Host}:{_dbConfig.Value.Port}/{_dbConfig.Value.Database}"),
      ImmutableList.Create(postgresDbConnectionTest, postgresDbTest),
      children: null
      );

    // Prometheus Checks
    var prometheusTestQuery = new HealthCheckModel(
      name: "test-query",
      description: "Prometheus Test Query",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Prometheus Exception upon querying",
        expression: null)
    );

    var prometheusReadiness = new HealthCheckModel(
      name: "readiness-probe",
      description: "Prometheus Readiness Endpoint",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Prometheus Readiness Endpoint and checks for Http Request Exception",
        expression: null)
    );

    var prometheusServiceConfig =
      new ServiceConfiguration(
        name: "prometheus",
        displayName: "Prometheus",
        description: "Prometheus Database",
        url: this._prometheusUrl,
        ImmutableList.Create(prometheusTestQuery, prometheusReadiness),
        children: null
      );

    var sonarRootServices =
      ImmutableHashSet<String>.Empty
        .Add("postgresql")
        .Add("prometheus");

    ServiceHierarchyConfiguration sonarConfiguration = new ServiceHierarchyConfiguration(
      ImmutableList.Create(
        postgresqlServiceConfig,
        prometheusServiceConfig
      ),
      sonarRootServices
    );

    return sonarConfiguration;
  }

  public async Task<ILookup<Guid, Guid>> GetServiceChildIdsLookup(
    ImmutableDictionary<Guid, Service> services,
    CancellationToken cancellationToken) {
    var serviceRelationships =
      await this.FetchExistingRelationships(services.Keys, cancellationToken);

    var serviceChildIdsLookup = serviceRelationships.ToLookup(
      keySelector: svc => svc.ParentServiceId,
      elementSelector: svc => svc.ServiceId
    );

    return serviceChildIdsLookup;
  }

  public async Task<Service?> GetSpecificService(
    String environment,
    String tenant,
    String servicePath,
    ILookup<Guid, Guid> serviceChildIds,
    CancellationToken cancellationToken) {

    // Validate root service
    var servicesInPath = servicePath.Split("/");
    var firstService = servicesInPath[0];
    Service existingService =
      await this.FetchExistingService(environment, tenant, firstService, cancellationToken);
    if (!existingService.IsRootService) {
      throw new ResourceNotFoundException(nameof(Service), firstService);
    }

    // If specified service is not root service, validate each subsequent service in given path
    if (servicesInPath.Length > 1) {
      var currParent = existingService;

      foreach (var currService in servicesInPath.Skip(1)) {
        // Ensure current service name matches an existing service
        existingService =
          await this.FetchExistingService(environment, tenant, currService, cancellationToken);

        // Ensure current service is a child of the current parent
        if (!(serviceChildIds[currParent.Id].Contains(existingService.Id))) {
          return null;
        }

        currParent = existingService;
      }
    }

    return existingService;
  }

  public async Task<List<Service>> TraverseServiceFamilyTree(
    Service existingService,
    ImmutableDictionary<Guid, Service> services,
    CancellationToken cancellationToken) {

    var serviceRelationships =
      await this.FetchExistingRelationships(services.Keys, cancellationToken);
    var serviceList = new List<Service>();
    // Add existing service
    serviceList.Add(existingService);
    // Get children of existing service.
    var children = GetChildren(serviceRelationships, existingService.Id);
    serviceList.AddRange(children.Select(x => services[x.ServiceId]));
    return serviceList;
  }

  public static List<ServiceRelationship> GetChildren(IList<ServiceRelationship> relationships, Guid id) {
    return relationships
      .Where(x => x.ParentServiceId == id)
      .Union(relationships.Where(x => x.ParentServiceId == id)
        .SelectMany(y => GetChildren(relationships, y.ServiceId))
      ).ToList();
  }

  public static ServiceHierarchyConfiguration Redact(ServiceHierarchyConfiguration config) {
    return config with {
      Services = config.Services.Select(Redact).ToImmutableList()
    };
  }

  private static ServiceConfiguration Redact(ServiceConfiguration serviceConfig) {
    return serviceConfig with {
      HealthChecks = serviceConfig.HealthChecks?.Select(Redact).ToImmutableList()
    };
  }

  private static HealthCheckModel Redact(HealthCheckModel healthCheck) {
    return healthCheck with {
      Definition = Redact(healthCheck.Definition)
    };
  }

  private static HealthCheckDefinition Redact(HealthCheckDefinition definition) {
    if (definition is HttpHealthCheckDefinition httpDef) {
      return httpDef with {
        AuthorizationHeader = httpDef.AuthorizationHeader != null ? new String(c: '*', count: 32) : null,
      };
    }

    return definition;
  }
}
