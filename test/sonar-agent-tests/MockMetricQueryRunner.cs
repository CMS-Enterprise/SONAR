using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Moq;
using TimeSeries = System.Collections.Immutable.IImmutableList<(System.DateTime Timestamp, System.Decimal Value)>;

namespace Cms.BatCave.Sonar.Agent.Tests;

public static class MockMetricQueryRunner {
  public static Mock<IMetricQueryRunner> CreateFixedReturnMock(TimeSeries result) {
    var mock = new Mock<IMetricQueryRunner>();

    mock.Setup(qr => qr.QueryRangeAsync(
        It.IsAny<String>(),
        It.IsAny<String>(),
        It.IsAny<DateTime>(),
        It.IsAny<DateTime>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync<String, String, DateTime, DateTime, CancellationToken, IMetricQueryRunner, TimeSeries?>(
        (name, query, start, end, token) => result
      );

    return mock;
  }

  public static Mock<IMetricQueryRunner> CreateFunctionalMock() {
    var mock = new Mock<IMetricQueryRunner>();

    mock.Setup(qr => qr.QueryRangeAsync(
        It.IsAny<String>(),
        It.IsAny<String>(),
        It.IsAny<DateTime>(),
        It.IsAny<DateTime>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync<String, String, DateTime, DateTime, CancellationToken, IMetricQueryRunner, TimeSeries?>(
        (name, query, start, end, token) => GenerateData(start, end).ToImmutableList()
      );

    return mock;
  }

  public static IEnumerable<(DateTime, Decimal)> GenerateData(DateTime start, DateTime end) {
    var dt = start;
    var rand = new Random();
    while (dt < end) {
      dt = dt.AddSeconds(1);
      yield return (dt, (Decimal)(rand.NextDouble() * rand.Next(Int32.MinValue, Int32.MaxValue)));
    }
  }
}
