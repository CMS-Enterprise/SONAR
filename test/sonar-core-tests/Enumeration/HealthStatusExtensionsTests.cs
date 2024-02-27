using Cms.BatCave.Sonar.Enumeration;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Enumeration;

public class HealthStatusExtensionsTests {

  [Theory]
  [InlineData(HealthStatus.Unknown, HealthStatus.Online)]
  [InlineData(HealthStatus.Unknown, HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Unknown, HealthStatus.Degraded)]
  [InlineData(HealthStatus.Unknown, HealthStatus.Offline)]
  [InlineData(HealthStatus.Offline, HealthStatus.Online)]
  [InlineData(HealthStatus.Offline, HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Offline, HealthStatus.Degraded)]
  [InlineData(HealthStatus.Degraded, HealthStatus.Online)]
  [InlineData(HealthStatus.Degraded, HealthStatus.AtRisk)]
  [InlineData(HealthStatus.AtRisk, HealthStatus.Online)]
  public void IsWorseThan_DifferentStatuses_ReturnsExpectedValue(HealthStatus moreSevere, HealthStatus lessSevere) {
    Assert.True(moreSevere.IsWorseThan(lessSevere));
    Assert.False(lessSevere.IsWorseThan(moreSevere));
  }

  [Theory]
  [InlineData(HealthStatus.Unknown)]
  [InlineData(HealthStatus.Offline)]
  [InlineData(HealthStatus.Degraded)]
  [InlineData(HealthStatus.AtRisk)]
  [InlineData(HealthStatus.Online)]
  public void IsWorseThan_SameStatus_ReturnsFalse(HealthStatus status) {
    Assert.False(status.IsWorseThan(status));
  }

}
