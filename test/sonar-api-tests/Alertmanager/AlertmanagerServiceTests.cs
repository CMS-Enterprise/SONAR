using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Alertmanager;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Alertmanager;

public class AlertmanagerServiceTests {

  private readonly AlertmanagerService _alertmanagerService;

  private Mock<IAlertmanagerClient> MockAlertmanagerClient { get; } = new();

  public AlertmanagerServiceTests() {
    this._alertmanagerService = new AlertmanagerService(
      logger: Mock.Of<ILogger<AlertmanagerService>>(),
      alertmanager: this.MockAlertmanagerClient.Object);
  }

  #region GetAlwaysFiringAlertStatusAsync Tests

  [Fact]
  public async Task GetAlwaysFiringAlertStatusAsync_AlwaysFiringAlertNotPresent_ReturnsOffline() {
    this.MockAlertmanagerClient
      .SetupGetAlertsAsyncForAlwaysFiringAlert()
      .ReturnsAsync(ImmutableList<GettableAlert>.Empty)
      .Verifiable();

    var status = await this._alertmanagerService.GetAlwaysFiringAlertStatusAsync(default);

    this.MockAlertmanagerClient.Verify();
    Assert.Equal(expected: HealthStatus.Offline, actual: status);
  }

  [Fact]
  public async Task GetAlwaysFiringAlertStatusAsync_AlwaysFiringAlertTooOld_ReturnsDegraded() {
    var staleTimeSpan = IPrometheusService.RuleEvaluationInterval + TimeSpan.FromSeconds(61);
    var staleAlert = Mock.Of<GettableAlert>(a => a.UpdatedAt == DateTimeOffset.Now.Subtract(staleTimeSpan));

    this.MockAlertmanagerClient
      .SetupGetAlertsAsyncForAlwaysFiringAlert()
      .ReturnsAsync(ImmutableList.Create(staleAlert))
      .Verifiable();

    var status = await this._alertmanagerService.GetAlwaysFiringAlertStatusAsync(default);

    this.MockAlertmanagerClient.Verify();
    Assert.Equal(expected: HealthStatus.Degraded, actual: status);
  }

  [Fact]
  public async Task GetAlwaysFiringAlertStatusAsync_AlwaysFiringAlertUpToDate_ReturnsOnline() {
    var freshTimeSpan = IPrometheusService.RuleEvaluationInterval;
    var freshAlert = Mock.Of<GettableAlert>(a => a.UpdatedAt == DateTimeOffset.Now.Subtract(freshTimeSpan));

    this.MockAlertmanagerClient
      .SetupGetAlertsAsyncForAlwaysFiringAlert()
      .ReturnsAsync(ImmutableList.Create(freshAlert))
      .Verifiable();

    var status = await this._alertmanagerService.GetAlwaysFiringAlertStatusAsync(default);

    this.MockAlertmanagerClient.Verify();
    Assert.Equal(expected: HealthStatus.Online, actual: status);
  }

  [Theory]
  [MemberData(nameof(GetAlwaysFiringAlertStatusAsync_AnyException_ReturnsUnknown_Data))]
  public async Task GetAlwaysFiringAlertStatusAsync_AnyExceptionWhileQueryingAlerts_ReturnsUnknown(
    Exception anyException) {

    this.MockAlertmanagerClient
      .SetupGetAlertsAsyncForAlwaysFiringAlert()
      .ThrowsAsync(anyException)
      .Verifiable();

    var status = await this._alertmanagerService.GetAlwaysFiringAlertStatusAsync(default);

    this.MockAlertmanagerClient.Verify();
    Assert.Equal(expected: HealthStatus.Unknown, actual: status);
  }

  public static IEnumerable<Object[]> GetAlwaysFiringAlertStatusAsync_AnyException_ReturnsUnknown_Data =>
    new List<Object[]> {
      new Object[] { new Exception() },
      new Object[] { new HttpRequestException() },
      new Object[] {
        new ApiException(
          message: "API Exception",
          statusCode: 500,
          response: "Internal Server Error",
          headers: ImmutableDictionary<String, IEnumerable<String>>.Empty,
          innerException: new Exception())
      }
    };

  #endregion GetAlwaysFiringAlertStatusAsync Tests
}

internal static class MockAlertmanagerClientExtensions {

  public static ISetup<IAlertmanagerClient, Task<ICollection<GettableAlert>>> SetupGetAlertsAsyncForAlwaysFiringAlert(
    this Mock<IAlertmanagerClient> mockAlertmanagerClient) {

    return mockAlertmanagerClient
      .Setup(c => c.GetAlertsAsync(
        true,
        true,
        true,
        true,
        new[] {
          $"{IAlertmanagerService.AlertNameLabel}={IAlertmanagerService.AlwaysFiringAlertName}"
        },
        ".*",
        It.IsAny<CancellationToken>()));
  }

}
