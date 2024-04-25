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
using Cms.BatCave.Sonar.Prometheus;
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
  private readonly DbSet<VersionCheck> _versionChecksTable;
  private readonly Uri _prometheusUrl;
  private readonly IOptions<DatabaseConfiguration> _dbConfig;
  private readonly IPrometheusService _prometheusService;

  public ServiceDataHelper(
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<ServiceRelationship> relationshipsTable,
    DbSet<HealthCheck> healthChecksTable,
    DbSet<VersionCheck> versionChecksTable,
    IOptions<PrometheusConfiguration> prometheusConfig,
    IOptions<DatabaseConfiguration> dbConfig,
    IPrometheusService prometheusService) {

    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._relationshipsTable = relationshipsTable;
    this._healthChecksTable = healthChecksTable;
    this._versionChecksTable = versionChecksTable;
    this._prometheusUrl = new Uri(
      $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}/"
    );
    this._dbConfig = dbConfig;
    this._prometheusService = prometheusService;
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

  public async Task<IList<Service>> FetchByServiceIdsAsync(
    List<Guid> ids,
    CancellationToken cancellationToken) {

    var result =
      await this._servicesTable.Where(e => ids.Contains(e.Id))
        .ToListAsync(cancellationToken);

    return result;
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

  public async Task<IList<VersionCheck>> FetchExistingVersionChecks(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return
      await this._versionChecksTable
        .Where(vc => serviceIds.Contains(vc.ServiceId))
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
        expression: null),
      null
      );

    var postgresDbTest = new HealthCheckModel(
      name: "sonar-database-test",
      description: "Database Error",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Checks for an Postgresql Exception",
        expression: null),
      null
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
        expression: null),
      null
    );

    // Prometheus Checks
    var prometheusWriteTest = new HealthCheckModel(
      name: "write-test",
      description: "Prometheus Remote Write Test",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Prometheus Exception when attempting to RemoteWrite",
        expression: null),
      null
    );

    var prometheusReadiness = new HealthCheckModel(
      name: "readiness-probe",
      description: "Prometheus Readiness Endpoint",
      HealthCheckType.Internal,
      new InternalHealthCheckDefinition(
        description: "Prometheus Readiness Endpoint and checks for Http Request Exception",
        expression: null),
      null
    );

    var prometheusServiceConfig =
      new ServiceConfiguration(
        name: "prometheus",
        displayName: "Prometheus",
        description: "Prometheus Database",
        url: this._prometheusUrl,
        ImmutableList.Create(
          prometheusTestQuery,
          prometheusWriteTest,
          prometheusReadiness),
        children: null
      );

    var alertmanagerConfigMapHealthCheck =
      new HealthCheckModel(
        name: "alertmanager-config",
        description: "Check whether the Alertmanager config map is up-to-date.",
        type: HealthCheckType.Internal,
        definition: new InternalHealthCheckDefinition(
          description: "Query the version annotation of the Alertmanager config map and verify it matches the " +
          "current alerting configuration in the SONAR database.",
          expression: null),
        smoothingTolerance: null);

    var prometheusRulesHealthCheck =
      new HealthCheckModel(
        name: "prometheus-rules",
        description: "Check whether the Prometheus config map is up-to-date.",
        type: HealthCheckType.Internal,
        definition: new InternalHealthCheckDefinition(
          description: "Query the version annotation of the Prometheus config map and verify it matches the " +
          "current alerting configuration in the SONAR database.",
          expression: null),
        smoothingTolerance: null);

    var alertmanagerSecretHealthCheck =
      new HealthCheckModel(
        name: "alertmanager-secret",
        description: "Check whether the Alertmanager secret is up-to-date.",
        type: HealthCheckType.Internal,
        definition: new InternalHealthCheckDefinition(
          description: "Query the version annotation of the Alertmanager secret and verify it matches the " +
          "current alerting configuration in the SONAR database.",
          expression: null),
        smoothingTolerance: null);

    var alertingConfigSyncServiceConfig =
      new ServiceConfiguration(
        name: "alertingconfig",
        displayName: "AlertingConfig",
        description: "The Kubernetes resources that SONAR API uses to persist Alertmanager configuration " +
        "for recipients and Prometheus rules.",
        healthChecks: ImmutableList.Create(
          alertmanagerConfigMapHealthCheck,
          prometheusRulesHealthCheck,
          alertmanagerSecretHealthCheck));

    var alwaysFiringAlertHealthCheck =
      new HealthCheckModel(
        name: "always-firing-alert",
        description: "Check whether Prometheus is evaluating alerting rules and sending alerts to Alertmanager.",
        type: HealthCheckType.Internal,
        definition: new InternalHealthCheckDefinition(
          description: "Query Alertmanager for the special always-firing alert and validate the alert is present " +
          "and was updated in the last Prometheus rule-evaluation interval.",
          expression: null),
        smoothingTolerance: null);

    var alertmanagerScrapingHealthCheck =
      new HealthCheckModel(
        name: "alertmanager-scraping",
        description: "Check whether Prometheus has up-to-date metrics for Alertmanager.",
        type: HealthCheckType.Internal,
        definition: new InternalHealthCheckDefinition(
          description: "Query Prometheus for Alertmanager's build_info metric and validate the most recent " +
          "sample of the metric was obtained in the last Prometheus scrape interval.",
          expression: null),
        smoothingTolerance: null);

    var emailNotificationsHealthCheck =
      new HealthCheckModel(
        name: "email-notifications",
        description: "Check whether Alertmanager has reported recent email notification failures in its metrics.",
        type: HealthCheckType.Internal,
        definition: new InternalHealthCheckDefinition(
          description: "Query Prometheus for the increase of Alertmanager's " +
          "alertmanager_notifications_failed_total{integration='email'} metric over the last few scape " +
          "intervals and validate the increase is zero (i.e. there are no recent failures).",
          expression: null),
        smoothingTolerance: null);

    var alertDeliveryServiceConfig =
      new ServiceConfiguration(
        name: "alert-delivery",
        displayName: "AlertDelivery",
        description: "The Prometheus and Alertmanager facilities used for SONAR service alert notification delivery.",
        healthChecks: ImmutableList.Create(
          alwaysFiringAlertHealthCheck,
          alertmanagerScrapingHealthCheck,
          emailNotificationsHealthCheck));

    var sonarRootServices =
      ImmutableHashSet<String>.Empty
        .Add("postgresql")
        .Add("prometheus")
        .Add("alertingconfig")
        .Add("alert-delivery");

    ServiceHierarchyConfiguration sonarConfiguration = new ServiceHierarchyConfiguration(
      services: ImmutableList.Create(
        postgresqlServiceConfig,
        prometheusServiceConfig,
        alertingConfigSyncServiceConfig,
        alertDeliveryServiceConfig
      ),
      rootServices: sonarRootServices
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

  public async Task<Dictionary<Guid, (Boolean IsInMaintenance, String? MaintenanceTypes)>> GetMaintenanceStatusByService(
    String environment,
    String tenant,
    ImmutableDictionary<Guid, Service> services,
    CancellationToken cancellationToken
  ) {
    var maintenanceStatusByServiceIdDictionary = new Dictionary<Guid, (Boolean IsInMaintenance, String? MaintenanceTypes)>();
    foreach (var service in services) {
      var maintenanceStatusData = await this._prometheusService
        .GetScopedCurrentMaintenanceStatus(
          environment,
          tenant,
          service.Value.Name,
          MaintenanceScope.Service,
          cancellationToken);
      maintenanceStatusByServiceIdDictionary.Add(service.Key, maintenanceStatusData);
    }

    return maintenanceStatusByServiceIdDictionary;
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
