using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Alerting.Internal;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Alerting;

public class AlertingRulesConfigurationGenerator {
  private readonly DataContext _context;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<Service> _servicesTable;
  private readonly DbSet<HealthCheck> _healthChecksTable;
  private readonly DbSet<ServiceRelationship> _relationshipsTable;
  private readonly DbSet<AlertingRule> _alertingRulesTable;
  private readonly DbSet<AlertReceiver> _alertReceiversTable;
  private readonly ErrorReportsDataHelper _errorReportsDataHelper;
  private readonly IOptions<WebHostConfiguration> _webHostConfiguration;
  private readonly ILogger<AlertingRulesConfigurationGenerator> _logger;

  public AlertingRulesConfigurationGenerator(
    DataContext context,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<Service> servicesTable,
    DbSet<HealthCheck> healthChecksTable,
    DbSet<ServiceRelationship> relationshipsTable,
    DbSet<AlertingRule> alertingRulesTable,
    DbSet<AlertReceiver> alertReceiversTable,
    ErrorReportsDataHelper errorReportsDataHelper,
    IOptions<WebHostConfiguration> webHostConfiguration,
    ILogger<AlertingRulesConfigurationGenerator> logger) {

    this._context = context;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._servicesTable = servicesTable;
    this._healthChecksTable = healthChecksTable;
    this._relationshipsTable = relationshipsTable;
    this._alertingRulesTable = alertingRulesTable;
    this._alertReceiversTable = alertReceiversTable;
    this._errorReportsDataHelper = errorReportsDataHelper;
    this._webHostConfiguration = webHostConfiguration;
    this._logger = logger;
  }

  internal async Task<(PrometheusAlertingConfiguration, AlertmanagerRoute, ImmutableList<AlertmanagerInhibitRuleConfiguration>)> GenerateAlertingRulesConfiguration(
    CancellationToken cancellationToken) {

    var allServicesList =
      await this._environmentsTable
        .Join(this._tenantsTable, e => e.Id, t => t.EnvironmentId, (e, t) => new { Environment = e, Tenant = t })
        .Join(this._servicesTable, r => r.Tenant.Id, s => s.TenantId,
          (r, s) => new { r.Environment, r.Tenant, Service = s })
        .LeftJoin(this._alertingRulesTable, r => r.Service.Id, a => a.ServiceId, (r, a) => new {
          r.Environment,
          r.Tenant,
          r.Service,
          AlertingRule = a
        })
        .ToListAsync(cancellationToken);

    var serviceHierarchy =
      allServicesList.GroupBy(svc => svc.Environment.Name)
        .ToDictionary(
          egrp => egrp.Key,
          egrp => new {
            egrp.First().Environment,
            Tenants = egrp.GroupBy(svc => svc.Tenant.Name)
              .ToDictionary(
                tgrp => tgrp.Key,
                tgrp => new {
                  tgrp.First().Tenant,
                  Services = tgrp.GroupBy(svc => svc.Service.Id)
                    .ToDictionary(
                      grp => grp.Key,
                      grp => new {
                        grp.First().Service,
                        AlertingRules = grp.Where(r => r.AlertingRule != null).Select(r => r.AlertingRule!).ToList()
                      })
                })
          });

    var serviceRelationships =
      (await this._relationshipsTable.ToListAsync(cancellationToken)).ToLookup(sr => sr.ParentServiceId,
        sr => sr.ServiceId);
    var serviceParentLookup =
      serviceRelationships.SelectMany(grp => grp.Select(childId => new { ChildId = childId, ParentId = grp.Key }))
        .ToLookup(rel => rel.ChildId, rel => rel.ParentId);

    var servicesWithHealthcheckLookup =
      (await this._healthChecksTable.Select(hc => hc.ServiceId).Distinct().ToListAsync(cancellationToken))
      .ToImmutableHashSet();

    var alertReceivers =
      (await this._alertReceiversTable.ToListAsync(cancellationToken)).ToImmutableDictionary(ar => ar.Id);

    var dashboardBaseUrl =
      this._webHostConfiguration.Value.AllowedOrigins?.FirstOrDefault() ??
        "http://localhost:8080";
    var resultGroups = new List<PrometheusAlertingGroup>();
    var routes = new List<AlertmanagerRoute>();
    var inhibitRules = new List<AlertmanagerInhibitRuleConfiguration>();
    foreach (var (environmentName, environment) in serviceHierarchy) {
      foreach (var (tenantName, tenant) in environment.Tenants) {
        var resultRules = new List<PrometheusAlertingRule>();
        // For all the services that have an alerting rule configured
        foreach (var rootAlertingSvc in tenant.Services.Values.Where(svc => svc.AlertingRules.Any())) {
          var servicePathStack = new Stack<String>();
          var child = rootAlertingSvc.Service;
          while (true) {
            servicePathStack.Push(child.Name);
            // Technically nothing prevents a service from having multiple parents. Which one we
            // pick is arbitrary.
            var parentId = serviceParentLookup[child.Id].FirstOrDefault();
            if (parentId != default) {
              child = tenant.Services[parentId].Service;
            } else {
              break;
            }
          }

          var servicePath = String.Join("/", servicePathStack);

          foreach (var alertingRule in rootAlertingSvc.AlertingRules) {
            var servicesWithHealthChecks = new List<Service>();
            // Traverse the service hierarchy and see which child services actually have health checks.
            var serviceQueue = MakeQueue(new[] { rootAlertingSvc });
            while (serviceQueue.Count > 0) {
              var currentService = serviceQueue.Dequeue();
              if (servicesWithHealthcheckLookup.Contains(currentService.Service.Id)) {
                servicesWithHealthChecks.Add(currentService.Service);
              }

              foreach (var childId in serviceRelationships[currentService.Service.Id]) {
                serviceQueue.Enqueue(tenant.Services[childId]);
              }
            }

            if (servicesWithHealthChecks.Any()) {
              var serviceNames = servicesWithHealthChecks.Select(svc => svc.Name).ToList();
              var expectedStatus = GetExpectedStatus(alertingRule.Threshold).ToList();

              if (expectedStatus.Any()) {
                // Construct PromQL
                var expr = new StringBuilder();
                // Of all of the relevant services, if any of them have a "bad" status, go with that
                expr.PromQlMin(
                  () =>
                    // For each child service, assuming there are
                    expr.PromQlMax(
                      () => {
                        expr
                          .PromQlSelector(
                            "sonar_service_status",
                            new PromQlLabelFilter("environment", PromQlOperator.Equal, environmentName),
                            new PromQlLabelFilter("tenant", PromQlOperator.Equal, tenantName),
                            new PromQlLabelFilter(
                              "service",
                              PromQlOperator.RegexMatch,
                              $"({String.Join('|', serviceNames)})"),
                            new PromQlLabelFilter(
                              "sonar_service_status",
                              PromQlOperator.RegexMatch,
                              $"({String.Join('|', expectedStatus)})"));
                        foreach (var svc in serviceNames) {
                          // Make sure there is a time series present for each service
                          // If any of actual status time series has a non zero value it will override this.
                          expr.Append(" or ")
                            .PromQlLabelReplace(
                              () => expr.Append("vector(0)"),
                              dest: "service",
                              replacement: svc,
                              original: "_",
                              match: ".*");
                        }
                      })
                    .PromQlBy("service")
                );

                expr.Append(" < 1");

                resultRules.Add(new PrometheusAlertingRule(
                  alertingRule.Name,
                  expr.ToString(),
                  TimeSpan.FromSeconds(alertingRule.Delay),
                  ImmutableDictionary<String, String>.Empty
                    .Add("environment", environmentName)
                    .Add("tenant", tenantName)
                    .Add("service", rootAlertingSvc.Service.Name)
                    .Add("threshold", alertingRule.Threshold.ToString()),
                  ImmutableDictionary<String, String>.Empty
                    .Add(
                      "sonar_dashboard_uri",
                      $"{dashboardBaseUrl}/{environmentName}/tenants/{tenantName}/services/{servicePath}")
                ));
                var receiver = alertReceivers[alertingRule.AlertReceiverId];
                routes.Add(new AlertmanagerRoute(
                  GenerateReceiverName(environmentName, tenantName, receiver),
                  ImmutableList.Create<String>(
                    $"environment=\"{environmentName}\"",
                    $"tenant=\"{tenantName}\"",
                    $"service=\"{rootAlertingSvc.Service.Name}\"",
                    $"alertname=\"{alertingRule.Name}\""),
                  ImmutableHashSet.Create<String>("environment", "tenant", "service")
                ));

                // Generate maintenance alerting rules and inhibitions for services
                foreach (var serviceInMaintenance in serviceNames) {
                  // Create in-maintenance rule
                  var maintenanceExpr = new StringBuilder();
                  maintenanceExpr.PromQlSelector(
                    metric: "sonar_service_maintenance_status",
                    selectors: new[] {
                      new PromQlLabelFilter("environment", PromQlOperator.Equal, environmentName),
                      new PromQlLabelFilter("tenant", PromQlOperator.Equal, tenantName),
                      new PromQlLabelFilter("service", PromQlOperator.Equal, serviceInMaintenance),
                    }
                  ).Append(" > 0");

                  var serviceInMaintenanceAlertName = $"{serviceInMaintenance}-is-in-maintenance";
                  resultRules.Add(new PrometheusAlertingRule(
                    serviceInMaintenanceAlertName,
                    maintenanceExpr.ToString(),
                    TimeSpan.FromSeconds(alertingRule.Delay),
                    ImmutableDictionary<String, String>.Empty
                      .Add("environment", environmentName)
                      .Add("tenant", tenantName)
                      .Add("service", rootAlertingSvc.Service.Name)
                      .Add("purpose", "maintenance"),
                    ImmutableDictionary<String, String>.Empty
                  ));

                  // Create inhibition rule
                  var sourceMatchers = new List<String> {
                    $"alertname={serviceInMaintenanceAlertName}"
                  }.ToImmutableList();
                  var targetMatchers = new List<String> {
                    $"alertname={alertingRule.Name}"
                  }.ToImmutableList();
                  var labels = new List<String> {
                    "environment",
                    "tenant",
                    "service"
                  }.ToImmutableList();

                  inhibitRules.Add(new AlertmanagerInhibitRuleConfiguration(
                    sourceMatchers,
                    targetMatchers,
                    labels));
                }
              } else {
                this._logger.LogWarning(
                  "Service '{Service}' has an invalid alert threshold: {Threshold} (Environment: {Environment}, Tenant: {Tenant})",
                  rootAlertingSvc.Service.Name,
                  alertingRule.Threshold,
                  environmentName,
                  tenantName
                );
                await this._errorReportsDataHelper.AddErrorReportAsync(
                  ErrorReport.New(
                    DateTime.UtcNow,
                    environment.Environment.Id,
                    tenant.Tenant.Id,
                    rootAlertingSvc.Service.Name,
                    healthCheckName: null,
                    AgentErrorLevel.Warning,
                    AgentErrorType.Validation,
                    $"Alerting rule {alertingRule.Name} has an invalid threshold: {alertingRule.Threshold}"
                  ),
                  cancellationToken
                );
              }
            } else {
              this._logger.LogWarning(
                "Service '{Service}' with alerting enabled has no health checks and no children with health checks (Environment: {Environment}, Tenant: {Tenant})",
                rootAlertingSvc.Service.Name,
                environmentName,
                tenantName
              );
              await this._errorReportsDataHelper.AddErrorReportAsync(
                ErrorReport.New(
                  DateTime.UtcNow,
                  environment.Environment.Id,
                  tenant.Tenant.Id,
                  rootAlertingSvc.Service.Name,
                  healthCheckName: null,
                  AgentErrorLevel.Warning,
                  AgentErrorType.Validation,
                  "Alerting enabled for a service with no health checks and no children with health checks"
                ),
                cancellationToken
              );
            }
          }
        }

        if (resultRules.Any()) {
          resultGroups.Add(new PrometheusAlertingGroup(
            $"{environmentName}_{tenantName}",
            resultRules.ToImmutableList()
          ));
        }
      }
    }

    await this._context.SaveChangesAsync(cancellationToken);

    routes.Add(new AlertmanagerRoute(
      Receiver: AlertingReceiverConfigurationGenerator.NullReceiverName,
      Matchers: ImmutableList.Create("alertname=\"always-firing\"")
    ));

    // route maintenance alerts to null receiver so that they don't trigger notifications
    routes.Add(new AlertmanagerRoute(
      Receiver: AlertingReceiverConfigurationGenerator.NullReceiverName,
      Matchers: ImmutableList.Create("purpose=\"maintenance\"")
    ));

    return (
      new PrometheusAlertingConfiguration(resultGroups.ToImmutableList()),
      new AlertmanagerRoute(
        Receiver: AlertingReceiverConfigurationGenerator.DefaultReceiverName,
        Matchers: ImmutableList<String>.Empty,
        GroupBy: ImmutableHashSet<String>.Empty,
        Routes: routes.ToImmutableList()),
      inhibitRules.ToImmutableList()
    );
  }

  // TODO: ensure this is used consistently between receiver creation and route creation
  public static String GenerateReceiverName(String environmentName, String tenantName, AlertReceiver receiver) {
    return $"{environmentName}_{tenantName}_{receiver.Name}";
  }

  private static Queue<T> MakeQueue<T>(IEnumerable<T> source) {
    return new Queue<T>(source);
  }

  private static IEnumerable<HealthStatus> GetExpectedStatus(HealthStatus threshold) {
    if (threshold == HealthStatus.Maintenance) {
      yield break;
    } else if (threshold == HealthStatus.Unknown) {
      // special case: if "Unknown" is the threshold then all normal status are expected
      for (var status = HealthStatus.Online; (status <= HealthStatus.Offline); status++) {
        yield return status;
      }

      yield break;
    }

    for (var status = HealthStatus.Online; (status < threshold) && (status <= HealthStatus.Offline); status++) {
      yield return status;
    }
  }
}
