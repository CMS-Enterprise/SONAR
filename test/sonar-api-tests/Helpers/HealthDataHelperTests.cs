using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using k8s.Autorest;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests.Helpers;

public class HealthDataHelperTests : ApiControllerTestsBase {
  private const String PathToFileWithBothAlertingConfigMaps = "test-inputs/both-alerting-configmaps.json";
  private const String PathToFileWithAlertmanagerConfigOnly = "test-inputs/alertmanager-configmap.json";
  private const String PathToFileWithAlertingRulesOnly = "test-inputs/alerting-rules.json";
  private const String PathToFileWithAlertingSecret = "test-inputs/alerting-secrets.json";
  private const String DbVersion1Timestamp = "2024-01-26T12:13:34Z";
  private const String KubernetesResourceTimestamp = "2024-01-26T22:13:34Z";
  private const String AlertmanagerConfigKey = "alertmanager-config";
  private const String PrometheusRulesKey = "prometheus-rules";
  private const String AlertmanagerSecretKey = "alertmanager-secret";
  private const Int32 SecondsWithinConfigSync = 299;
  private const Int32 SecondsOutsideConfigSync = 301;

  public HealthDataHelperTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper) : base(fixture, outputHelper, true) {
  }

  #region One Alerting Kubernetes Resource Exists
  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_NoAlertingK8sResources() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          null,
          null,
          null);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Offline, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Offline, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Offline, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }

  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_OnlyAlertmanagerConfigExists() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithAlertmanagerConfigOnly);

      Assert.Single(listNamespacedConfigMapResponse.Body.Items);
      var alertmanagerConfigMap = listNamespacedConfigMapResponse.Body.Items[0];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          alertmanagerConfigMap,
          null,
          null);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Online, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Offline, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Offline, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }

  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_OnlyAlertingRulesExists() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithAlertingRulesOnly);

      Assert.Single(listNamespacedConfigMapResponse.Body.Items);
      var prometheusAlertingRulesConfigMap = listNamespacedConfigMapResponse.Body.Items[0];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          null,
          prometheusAlertingRulesConfigMap,
          null);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Offline, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Online, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Offline, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }

  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_OnlyAlertmanagerSecretExists() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedSecretResponse = await this.GetSecretList();

      Assert.Single(listNamespacedSecretResponse.Body.Items);
      var alertmanagerSecret = listNamespacedSecretResponse.Body.Items[0];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          null,
          null,
          alertmanagerSecret);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Offline, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Offline, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Online, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }
  #endregion

  #region Two Alerting Kubernetes Resources Exist
  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_OnlyAlertingConfigMapsExist() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithBothAlertingConfigMaps);

      Assert.Equal(2, listNamespacedConfigMapResponse.Body.Items.Count);
      var alertmanagerConfigMap = listNamespacedConfigMapResponse.Body.Items[0];
      var prometheusAlertingRulesConfigMap = listNamespacedConfigMapResponse.Body.Items[1];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          alertmanagerConfigMap,
          prometheusAlertingRulesConfigMap,
          null);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Online, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Online, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Offline, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }

  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_OnlyAlertmanagerConfigAndSecretExist() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithAlertmanagerConfigOnly);
      var listNamespacedSecretResponse = await this.GetSecretList();

      Assert.Single(listNamespacedConfigMapResponse.Body.Items);
      Assert.Single(listNamespacedSecretResponse.Body.Items);
      var alertmanagerConfigMap = listNamespacedConfigMapResponse.Body.Items[0];
      var alertmanagerSecret = listNamespacedSecretResponse.Body.Items[0];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          alertmanagerConfigMap,
          null,
          alertmanagerSecret);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Online, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Offline, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Online, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }

  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_OnlyAlertingRulesAndAlertmanagerSecretExist() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithAlertingRulesOnly);
      var listNamespacedSecretResponse = await this.GetSecretList();

      Assert.Single(listNamespacedConfigMapResponse.Body.Items);
      Assert.Single(listNamespacedSecretResponse.Body.Items);
      var prometheusAlertingRulesConfigMap = listNamespacedConfigMapResponse.Body.Items[0];
      var alertmanagerSecret = listNamespacedSecretResponse.Body.Items[0];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          null,
          prometheusAlertingRulesConfigMap,
          alertmanagerSecret);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Offline, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Online, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Online, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Offline, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }
  #endregion

  #region All Alerting Kubernetes Resources Exist
  [Fact]
  public async Task GetAlertingHealthChecksAndAggStatus_VersionInK8sResourcesIsMoreUpToDateThanDb() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithBothAlertingConfigMaps);
      var listNamespacedSecretResponse = await this.GetSecretList();

      Assert.Equal(2, listNamespacedConfigMapResponse.Body.Items.Count);
      Assert.Single(listNamespacedSecretResponse.Body.Items);

      var alertmanagerConfigMap = listNamespacedConfigMapResponse.Body.Items[0];
      var prometheusAlertingRulesConfigMap = listNamespacedConfigMapResponse.Body.Items[1];
      var alertmanagerSecret = listNamespacedSecretResponse.Body.Items[0];

      var currentTimestamp = DateTime.UtcNow;
      var latestAlertingConfigVersion = new AlertingConfigurationVersion(
        DateTime.Parse(DbVersion1Timestamp).ToUniversalTime());

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          alertmanagerConfigMap,
          prometheusAlertingRulesConfigMap,
          alertmanagerSecret);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(HealthStatus.Online, alertmanagerConfigStatus);
      Assert.Equal(HealthStatus.Online, prometheusRulesStatus);
      Assert.Equal(HealthStatus.Online, alertmanagerSecretStatus);
      Assert.Equal(HealthStatus.Online, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }

  [Theory]
  [InlineData(SecondsWithinConfigSync, HealthStatus.AtRisk)]
  [InlineData(SecondsOutsideConfigSync, HealthStatus.Degraded)]
  public async Task GetAlertingHealthChecksAndAggStatus_VersionInDbIsMoreUpToDateThanK8sResources(
    Double numberOfSeconds,
    HealthStatus expectedHealthStatus) {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var dbContext = services.GetRequiredService<DataContext>();
      var dbAlertingConfigVersion = services.GetRequiredService<DbSet<AlertingConfigurationVersion>>();
      var alertingDataHelper = services.GetRequiredService<AlertingDataHelper>();
      var healthDataHelper = services.GetRequiredService<HealthDataHelper>();

      // Have AlertingConfigurationVersion DB's latest versionNumber be more recent than Kubernetes resources'
      var dbTimeStamp = DateTime.Parse(DbVersion1Timestamp).ToUniversalTime();
      await dbAlertingConfigVersion.AddAsync(new AlertingConfigurationVersion(dbTimeStamp), cancellationToken);
      dbTimeStamp = DateTime.Parse(KubernetesResourceTimestamp).ToUniversalTime();
      await dbAlertingConfigVersion.AddAsync(new AlertingConfigurationVersion(dbTimeStamp), cancellationToken);
      dbTimeStamp = dbTimeStamp.AddSeconds(125);
      await dbAlertingConfigVersion.AddAsync(new AlertingConfigurationVersion(dbTimeStamp), cancellationToken);
      await dbContext.SaveChangesAsync(cancellationToken);

      var latestAlertingConfigVersion = await alertingDataHelper
        .FetchLatestAlertingConfigVersionAsync(cancellationToken);

      Assert.NotNull(latestAlertingConfigVersion);

      var listNamespacedConfigMapResponse = await this.GetConfigMapList(PathToFileWithBothAlertingConfigMaps);
      var listNamespacedSecretResponse = await this.GetSecretList();

      Assert.Equal(2, listNamespacedConfigMapResponse.Body.Items.Count);
      var alertmanagerConfigMap = listNamespacedConfigMapResponse.Body.Items[0];
      var prometheusAlertingRulesConfigMap = listNamespacedConfigMapResponse.Body.Items[1];
      Assert.Single(listNamespacedSecretResponse.Body.Items);
      var alertmanagerSecret = listNamespacedSecretResponse.Body.Items[0];

      // current timestamp is past the last possible alerting config sync
      var currentTimestamp = dbTimeStamp.AddSeconds(numberOfSeconds);

      var healthChecks = healthDataHelper
        .GetAlertingConfigSyncStatusAsync(
          currentTimestamp,
          latestAlertingConfigVersion,
          alertmanagerConfigMap,
          prometheusAlertingRulesConfigMap,
          alertmanagerSecret);

      var (_, alertmanagerConfigStatus) = healthChecks[AlertmanagerConfigKey].Value;
      var (_, prometheusRulesStatus) = healthChecks[PrometheusRulesKey].Value;
      var (_, alertmanagerSecretStatus) = healthChecks[AlertmanagerSecretKey].Value;

      Assert.Equal(expectedHealthStatus, alertmanagerConfigStatus);
      Assert.Equal(expectedHealthStatus, prometheusRulesStatus);
      Assert.Equal(expectedHealthStatus, alertmanagerSecretStatus);
      Assert.Equal(expectedHealthStatus, HealthDataHelper.GetAggregateHealthStatus(healthChecks));
    });
  }
  #endregion

  [Theory]
  [MemberData(nameof(GetAggregateHealthStatus_ReturnsExpectedResult_Data))]
  public void GetAggregateHealthStatus_ReturnsExpectedResult(
    IDictionary<String, (DateTime, HealthStatus)?> healthChecks,
    HealthStatus expectedStatus) {

    Assert.Equal(expected: expectedStatus, actual: HealthDataHelper.GetAggregateHealthStatus(healthChecks));
  }

  public static IEnumerable<Object[]> GetAggregateHealthStatus_ReturnsExpectedResult_Data =>
    new List<Object[]> {
      // Empty health checks collection -> Online
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?>(),
        HealthStatus.Online
      },
      // Health check missing the (DateTime, HealthStatus) tuple -> Unknown
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?> {
          ["health-check-1"] = (DateTime.UtcNow, HealthStatus.Online),
          ["health-check-2"] = (DateTime.UtcNow, HealthStatus.Online),
          ["health-check-3"] = null,
        },
        HealthStatus.Unknown
      },
      // All health checks online -> Online
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?> {
          ["health-check-1"] = (DateTime.UtcNow, HealthStatus.Online),
          ["health-check-2"] = (DateTime.UtcNow, HealthStatus.Online),
          ["health-check-3"] = (DateTime.UtcNow, HealthStatus.Online),
        },
        HealthStatus.Online
      },
      // Worst health checks is AtRisk -> AtRisk
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?> {
          ["health-check-1"] = (DateTime.UtcNow, HealthStatus.Online),
          ["health-check-2"] = (DateTime.UtcNow, HealthStatus.AtRisk),
          ["health-check-3"] = (DateTime.UtcNow, HealthStatus.Online),
        },
        HealthStatus.AtRisk
      },
      // Worst health checks is Degraded -> Degraded
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?> {
          ["health-check-1"] = (DateTime.UtcNow, HealthStatus.Degraded),
          ["health-check-2"] = (DateTime.UtcNow, HealthStatus.AtRisk),
          ["health-check-3"] = (DateTime.UtcNow, HealthStatus.Online),
        },
        HealthStatus.Degraded
      },
      // Worst health checks is Offline -> Offline
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?> {
          ["health-check-1"] = (DateTime.UtcNow, HealthStatus.Degraded),
          ["health-check-2"] = (DateTime.UtcNow, HealthStatus.Offline),
          ["health-check-3"] = (DateTime.UtcNow, HealthStatus.AtRisk),
        },
        HealthStatus.Offline
      },
      // Worst health checks is Unknown -> Unknown
      new Object[] {
        new Dictionary<String, (DateTime, HealthStatus)?> {
          ["health-check-1"] = (DateTime.UtcNow, HealthStatus.Degraded),
          ["health-check-2"] = (DateTime.UtcNow, HealthStatus.Offline),
          ["health-check-3"] = (DateTime.UtcNow, HealthStatus.Unknown),
        },
        HealthStatus.Unknown
      }
    };

  internal async Task<HttpOperationResponse<V1ConfigMapList>> GetConfigMapList(String configmapFilePath) {
    String alertingConfigJsonContents;

    await using var jsonStreamAlertmanagerConfig =
      new FileStream(configmapFilePath, FileMode.Open, FileAccess.Read);

    using (StreamReader reader = new StreamReader(jsonStreamAlertmanagerConfig)) {
      alertingConfigJsonContents = reader.ReadToEnd();
    }

    var listNamespacedConfigMapResponse = new HttpOperationResponse<V1ConfigMapList>();
    JsonDocument configMapJsonDoc = JsonDocument.Parse(alertingConfigJsonContents);
    listNamespacedConfigMapResponse.Body = configMapJsonDoc.RootElement.Deserialize<V1ConfigMapList>();

    return listNamespacedConfigMapResponse;
  }

  internal async Task<HttpOperationResponse<V1SecretList>> GetSecretList() {
    String secretsJsonContents;

    await using var jsonStreamSecrets =
      new FileStream(PathToFileWithAlertingSecret, FileMode.Open, FileAccess.Read);

    using (StreamReader reader = new StreamReader(jsonStreamSecrets)) {
      secretsJsonContents = reader.ReadToEnd();
    }

    var listNamespacedSecretResponse = new HttpOperationResponse<V1SecretList>();
    JsonDocument secretJsonDoc = JsonDocument.Parse(secretsJsonContents);
    listNamespacedSecretResponse.Body = secretJsonDoc.RootElement.Deserialize<V1SecretList>();

    return listNamespacedSecretResponse;
  }
}
