using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Alertmanager;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests;

public class ServiceAlertControllerTests : ApiControllerTestsBase {

  private readonly ITestOutputHelper _output;

  private readonly Mock<IAlertmanagerService> _mockAlertmanagerService = new();
  public ServiceAlertControllerTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper output)
    : base(fixture, output, resetDatabase: true) {
    this._output = output;
  }

  protected override void OnInitializing(WebApplicationBuilder builder) {
    base.OnInitializing(builder);
    builder.Services.RemoveAll<IAlertmanagerService>();
    builder.Services.AddScoped<IAlertmanagerService>(_ => this._mockAlertmanagerService.Object);
  }

  [Fact]
  public async Task GetServiceAlerts_MissingEnvironment_ReturnsNotFoundResponse() {
    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/not_there/tenants/dont_care/services/dont_care")
      .GetAsync();

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetServiceAlerts_MissingTenant_ReturnsNotFoundResponse() {
    var (environmentName, _) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet<String>.Empty,
        services: ImmutableList<ServiceConfiguration>.Empty));

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/not_there/services/dont_care")
      .GetAsync();

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetServiceAlerts_MissingService_ReturnsNotFoundResponse() {
    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet<String>.Empty,
        services: ImmutableList<ServiceConfiguration>.Empty));

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/not_there")
      .GetAsync();

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetServiceAlerts_AlertmanagerTimeout_ReturnsGatewayTimeoutResponse() {
    const String serviceName = "service-1";

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName))));

    this._mockAlertmanagerService
      .Setup(s => s.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ThrowsAsync(new TaskCanceledException())
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    this._mockAlertmanagerService.Verify();
    Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
  }

  [Fact]
  public async Task GetServiceAlerts_NoAlertingRulesDefined_ReturnsEmptyList() {
    const String serviceName = "service-1";

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName))));

    this._mockAlertmanagerService
      .Setup(s => s.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList<GettableAlert>.Empty)
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    var serviceAlerts = await response.Content.ReadFromJsonAsync<ServiceAlert[]>(SerializerOptions);

    this._mockAlertmanagerService.Verify();
    Assert.NotNull(serviceAlerts);
    Assert.Empty(serviceAlerts);
  }

  [Fact]
  public async Task GetServiceAlerts_NoAlertsFiring_ReturnsExpectedState() {
    const String serviceName = "service-1";
    const String alertReceiverName = "receiver-1";

    var alertingRules = ImmutableList.Create(
      new AlertingRuleConfiguration(
        name: "rule-1",
        threshold: HealthStatus.Degraded,
        receiverName: alertReceiverName),
      new AlertingRuleConfiguration(
        name: "rule-2",
        threshold: HealthStatus.Offline,
        receiverName: alertReceiverName));

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName,
            alertingRules: alertingRules)),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: alertReceiverName,
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "test@test.test"))))));

    this._mockAlertmanagerService
      .Setup(a => a.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList<GettableAlert>.Empty)
      .Verifiable();

    this._mockAlertmanagerService.Setup(
        a => a.GetActiveServiceSilencesAsync(
          environmentName,
          tenantName,
          serviceName,
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList<GettableSilence>.Empty)
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    var serviceAlerts = await response.Content.ReadFromJsonAsync<ServiceAlert[]>(SerializerOptions);

    this._mockAlertmanagerService.Verify();
    Assert.NotNull(serviceAlerts);
    Assert.Equal(expected: alertingRules.Count, actual: serviceAlerts.Length);
    Assert.All(alertingRules, action: r => Assert.Contains(r.Name, serviceAlerts.Select(a => a.Name)));
    Assert.All(serviceAlerts, action: a => {
      Assert.False(a.IsFiring);
      Assert.Null(a.Since);
    });
  }

  [Fact]
  public async Task GetServiceAlerts_AlertsFiring_ReturnsExpectedState() {
    const String serviceName = "service-1";
    const String alertReceiverName = "receiver-1";

    var alertingRules = ImmutableList.Create(
      new AlertingRuleConfiguration(
        name: "rule-1",
        threshold: HealthStatus.Degraded,
        receiverName: alertReceiverName),
      new AlertingRuleConfiguration(
        name: "rule-2",
        threshold: HealthStatus.Offline,
        receiverName: alertReceiverName));

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName,
            alertingRules: alertingRules)),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: alertReceiverName,
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "test@test.test"))))));

    var activeAlertStartsAt = DateTimeOffset.UtcNow;

    this._mockAlertmanagerService
      .Setup(a => a.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        alertingRules.Select(r =>
            new GettableAlert {
              Labels = new LabelSet { [IAlertmanagerService.AlertNameLabel] = r.Name },
              StartsAt = activeAlertStartsAt
            })
          .ToImmutableList())
      .Verifiable();

    this._mockAlertmanagerService.Setup(
      a => a.GetActiveServiceSilencesAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList<GettableSilence>.Empty)
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    var serviceAlerts = await response.Content.ReadFromJsonAsync<ServiceAlert[]>(SerializerOptions);

    this._mockAlertmanagerService.Verify();
    Assert.NotNull(serviceAlerts);
    Assert.Equal(expected: alertingRules.Count, actual: serviceAlerts.Length);
    Assert.All(alertingRules, action: r => Assert.Contains(r.Name, serviceAlerts.Select(a => a.Name)));
    Assert.All(serviceAlerts, action: a => {
      Assert.True(a.IsFiring);
      Assert.Equal(expected: activeAlertStartsAt.DateTime, actual: a.Since);
    });
  }

  [Fact]
  public async Task GetServiceAlerts_AlertsFiringWithSilences_ReturnsExpectedState() {
    const String serviceName = "service-1";
    const String alertReceiverName = "receiver-1";
    const String alertRule1Name = "rule-1";
    const String alertRule2Name = "rule-2";
    const String alertSilencedBy = "me";

    var start = DateTime.UtcNow;
    var end = start.AddDays(1);
    var alertingRules = ImmutableList.Create(
      new AlertingRuleConfiguration(
        name: alertRule1Name,
        threshold: HealthStatus.Degraded,
        receiverName: alertReceiverName),
      new AlertingRuleConfiguration(
        name: alertRule2Name,
        threshold: HealthStatus.Offline,
        receiverName: alertReceiverName));

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName,
            alertingRules: alertingRules)),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: alertReceiverName,
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "test@test.test"))))));

    var activeAlertStartsAt = DateTimeOffset.UtcNow;

    this._mockAlertmanagerService
      .Setup(a => a.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        alertingRules.Select(r =>
            new GettableAlert {
              Labels = new LabelSet { [IAlertmanagerService.AlertNameLabel] = r.Name },
              StartsAt = activeAlertStartsAt
            })
          .ToImmutableList())
      .Verifiable();

    this._mockAlertmanagerService.Setup(
      a => a.GetActiveServiceSilencesAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList.Create(
        new GettableSilence() {
          Comment = "Comment",
          CreatedBy = alertSilencedBy,
          StartsAt = start,
          EndsAt = end,
          Id = "1",
          Matchers = new Matchers() {
            new Matcher() { Name = IAlertmanagerService.AlertNameLabel, Value = alertRule1Name, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.EnvironmentLabel, Value = environmentName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.TenantLabel, Value = tenantName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.ServiceLabel, Value = serviceName, IsRegex = false }
          }
        },
        new GettableSilence() {
          Comment = "Comment",
          CreatedBy = alertSilencedBy,
          StartsAt = start,
          EndsAt = end,
          Id = "2",
          Matchers = new Matchers() {
            new Matcher() { Name = IAlertmanagerService.AlertNameLabel, Value = alertRule2Name, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.EnvironmentLabel, Value = environmentName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.TenantLabel, Value = tenantName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.ServiceLabel, Value = serviceName, IsRegex = false }
          }
        }
        ))
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    var serviceAlerts = await response.Content.ReadFromJsonAsync<ServiceAlert[]>(SerializerOptions);

    this._mockAlertmanagerService.Verify();
    Assert.NotNull(serviceAlerts);
    Assert.All(serviceAlerts, action: a => {
      Assert.True(a.IsSilenced);
      Assert.NotNull(a.SilenceDetails);
      Assert.Equal(expected: start, actual: a.SilenceDetails.StartsAt);
      Assert.Equal(expected: end, actual: a.SilenceDetails.EndsAt);
      Assert.Equal(expected: alertSilencedBy, actual: a.SilenceDetails.SilencedBy);
    });
  }

  // Tests that the GetServiceAlerts action correctly selects the most recently updated silence for an alert.
  [Fact]
  public async Task GetServiceAlerts_MultipleSilences_ReturnsExpectedState() {
    const String serviceName = "service-1";
    const String alertReceiverName = "receiver-1";
    const String alertRule1Name = "rule-1";
    const String alertSilencedBy = "me";
    const String updatedSilencedBy = "you";

    var start = DateTime.UtcNow;
    var end = start.AddDays(1);
    var updatedAtEarly = start;
    var updatedAtLater = start.AddHours(1);


    var alertingRules = ImmutableList.Create(
      new AlertingRuleConfiguration(
        name: alertRule1Name,
        threshold: HealthStatus.Degraded,
        receiverName: alertReceiverName));

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName,
            alertingRules: alertingRules)),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: alertReceiverName,
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "test@test.test"))))));

    var activeAlertStartsAt = DateTimeOffset.UtcNow;

    this._mockAlertmanagerService
      .Setup(a => a.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        alertingRules.Select(r =>
            new GettableAlert {
              Labels = new LabelSet { [IAlertmanagerService.AlertNameLabel] = r.Name },
              StartsAt = activeAlertStartsAt
            })
          .ToImmutableList())
      .Verifiable();

    this._mockAlertmanagerService.Setup(
      a => a.GetActiveServiceSilencesAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList.Create(
        new GettableSilence() {
          Comment = "Comment",
          CreatedBy = alertSilencedBy,
          StartsAt = start,
          EndsAt = end,
          Id = "1",
          UpdatedAt = updatedAtEarly,
          Matchers = new Matchers() {
            new Matcher() { Name = IAlertmanagerService.AlertNameLabel, Value = alertRule1Name, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.EnvironmentLabel, Value = environmentName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.TenantLabel, Value = tenantName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.ServiceLabel, Value = serviceName, IsRegex = false }
          }
        },
        new GettableSilence() {
          Comment = "Comment",
          CreatedBy = updatedSilencedBy,
          StartsAt = start,
          EndsAt = end,
          Id = "2",
          UpdatedAt = updatedAtLater,
          Matchers = new Matchers() {
            new Matcher() { Name = IAlertmanagerService.AlertNameLabel, Value = alertRule1Name, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.EnvironmentLabel, Value = environmentName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.TenantLabel, Value = tenantName, IsRegex = false },
            new Matcher() { Name = IAlertmanagerService.ServiceLabel, Value = serviceName, IsRegex = false }
          }
        }
        ))
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    var serviceAlerts = await response.Content.ReadFromJsonAsync<ServiceAlert[]>(SerializerOptions);

    this._mockAlertmanagerService.Verify();
    Assert.NotNull(serviceAlerts);
    var alert = Assert.Single(serviceAlerts);
    Assert.NotNull(alert.SilenceDetails);
    Assert.Equal(expected: updatedSilencedBy, actual: alert.SilenceDetails.SilencedBy);
  }

  [Fact]
  public async Task GetServiceAlerts_AlertsFiringWithNoSilences_ReturnsExpectedState() {
    const String serviceName = "service-1";
    const String alertReceiverName = "receiver-1";
    const String alertRule1Name = "rule-1";
    const String alertRule2Name = "rule-2";
    const String alertSilencedBy = "me";

    var start = DateTime.UtcNow;
    var end = start.AddDays(1);
    var alertingRules = ImmutableList.Create(
      new AlertingRuleConfiguration(
        name: alertRule1Name,
        threshold: HealthStatus.Degraded,
        receiverName: alertReceiverName),
      new AlertingRuleConfiguration(
        name: alertRule2Name,
        threshold: HealthStatus.Offline,
        receiverName: alertReceiverName));

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName,
            alertingRules: alertingRules)),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: alertReceiverName,
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "test@test.test"))))));

    var activeAlertStartsAt = DateTimeOffset.UtcNow;

    this._mockAlertmanagerService
      .Setup(a => a.GetActiveAlertsAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        alertingRules.Select(r =>
            new GettableAlert {
              Labels = new LabelSet { [IAlertmanagerService.AlertNameLabel] = r.Name },
              StartsAt = activeAlertStartsAt
            })
          .ToImmutableList())
      .Verifiable();

    this._mockAlertmanagerService.Setup(
      a => a.GetActiveServiceSilencesAsync(
        environmentName,
        tenantName,
        serviceName,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(ImmutableList<GettableSilence>.Empty)
      .Verifiable();

    var response = await this.Fixture.Server
      .CreateRequest($"/api/v2/alerts/{environmentName}/tenants/{tenantName}/services/{serviceName}")
      .GetAsync();

    var serviceAlerts = await response.Content.ReadFromJsonAsync<ServiceAlert[]>(SerializerOptions);

    this._mockAlertmanagerService.Verify();
    Assert.NotNull(serviceAlerts);
    Assert.All(serviceAlerts, action: a => {
      Assert.False(a.IsSilenced);
      Assert.Null(a.SilenceDetails);
    });
  }

  [Fact]
  public async Task CreateAlertSilence_Success() {
    var (environment, tenant, service, alert) = await CreateBasicAlertingConfiguration();
    var user = await this.Fixture.CreateGlobalAdminUser();
    var createSilenceResponse = await this.Fixture.CreateFakeJwtRequest($"api/v2/alerts/silences/{environment}/tenants/{tenant}/services/{service}", user.Email)
      .And(req => {
        req.Content = JsonContent.Create(new {
          Name = alert
        });
      }).PostAsync();
    Assert.Equal(expected: HttpStatusCode.NoContent, createSilenceResponse.StatusCode);
  }

  [Fact]
  public async Task RemoveAlertSilence_Success() {
    var (environment, tenant, service, alert) = await CreateBasicAlertingConfiguration();
    var user = await this.Fixture.CreateGlobalAdminUser();
    var removeSilenceResponse = await this.Fixture.CreateFakeJwtRequest($"api/v2/alerts/silences/{environment}/tenants/{tenant}/services/{service}", user.Email)
      .And(req => {
        req.Content = JsonContent.Create(new {
          Name = alert
        });
      }).SendAsync("PUT");
    Assert.Equal(expected: HttpStatusCode.NoContent, removeSilenceResponse.StatusCode);
  }

  private async Task<(String environment, String tenant, String service, String alertName)>
    CreateBasicAlertingConfiguration() {
    const String serviceName = "service-1";
    const String alertReceiverName = "receiver-1";
    const String alertName = "rule-1";

    var alertingRules = ImmutableList.Create(
      new AlertingRuleConfiguration(
        name: alertName,
        threshold: HealthStatus.Degraded,
        receiverName: alertReceiverName));

    var (environmentName, tenantName) = await this.CreateTestConfiguration(
      new ServiceHierarchyConfiguration(
        rootServices: ImmutableHashSet.Create(serviceName),
        services: ImmutableList.Create(
          new ServiceConfiguration(
            name: serviceName,
            displayName: serviceName,
            alertingRules: alertingRules)),
        alerting: new AlertingConfiguration(
          receivers: ImmutableList.Create(
            new AlertReceiverConfiguration(
              name: alertReceiverName,
              receiverType: AlertReceiverType.Email,
              options: new AlertReceiverOptionsEmail(
                address: "test@test.test"))))));
    return (environmentName, tenantName, serviceName, alertName);
  }

}
