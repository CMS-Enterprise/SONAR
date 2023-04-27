using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class ReportingMetricQueryRunnerUnitTest {
  public static readonly IEnumerable<Object[]> HandledErrorTypes = new List<Object[]> {
    new Object[] {
      new ApiException<ProblemDetails>(
        "test error",
        (Int32)HttpStatusCode.BadRequest,
        "Bad Request",
        new Dictionary<String, IEnumerable<String>>(),
        new ProblemDetails {
          Detail = "test error"
        },
        null)
    },
    new Object[] {
      new HttpRequestException("network error")
    },
    new Object[] {
      new TaskCanceledException("http timeout")
    }
  };

  public static readonly IEnumerable<Object[]> UnhandledErrors = new List<Object[]> {
    new Object[] {
      new InvalidOperationException("unknown error"), false
    },
    new Object[] {
      new TaskCanceledException("task canceled"), true
    }
  };

  [Fact]
  public async Task Query_ReportsMetrics() {
    var hc = $"{Guid.NewGuid()}";
    var hcId = new HealthCheckIdentifier("env", "ten", "svc", hc);
    var expr = $"{Guid.NewGuid()}";

    var end = DateTime.UtcNow;
    var start = end.Subtract(TimeSpan.FromMinutes(10));
    var data = MockMetricQueryRunner.GenerateData(start, end).ToImmutableList();

    var mockRunner = MockMetricQueryRunner.CreateFixedReturnMock(data);
    var mockDisposable = new Mock<IDisposable>(MockBehavior.Loose);
    var mockSonarApi = new Mock<ISonarClient>(MockBehavior.Loose);

    var reportingRunner = new ReportingMetricQueryRunner(
      mockRunner.Object,
      () => (mockDisposable.Object, mockSonarApi.Object),
      Mock.Of<ILogger<ReportingMetricQueryRunner>>()
    );

    var tokenSource = new CancellationTokenSource();

    var result = await reportingRunner.QueryRangeAsync(
      hcId,
      expr,
      start,
      end,
      tokenSource.Token
    );

    // Verify the inner IMetricQueryRunner is called with the original start and end date
    mockRunner.Verify(qr => qr.QueryRangeAsync(hcId, expr, start, end, tokenSource.Token));

    mockSonarApi.Verify(api => api.RecordHealthCheckDataAsync(
      hcId.Environment,
      hcId.Tenant,
      hcId.Service,
      It.Is<ServiceHealthData>(recorded => Equivalent(hcId.Name, data, recorded)),
      tokenSource.Token
    ));

    // The data should be returned as is
    Assert.NotNull(result);
    Assert.Equal(data, result);
  }

  // Test ApiException raised by RecordHealthCheckDataAsync does not prevent data from being returned
  // Same for HttpRequestException and TaskCancellationException due to Http timeout
  [Theory]
  [MemberData(nameof(HandledErrorTypes))]
  public async Task Query_ReportMetrics_HandledException(Exception ex) {
    var hc = $"{Guid.NewGuid()}";
    var hcId = new HealthCheckIdentifier("env", "ten", "svc", hc);
    var expr = $"{Guid.NewGuid()}";

    var end = DateTime.UtcNow;
    var start = end.Subtract(TimeSpan.FromMinutes(10));
    var data = MockMetricQueryRunner.GenerateData(start, end).ToImmutableList();

    var mockRunner = MockMetricQueryRunner.CreateFixedReturnMock(data);
    var mockDisposable = new Mock<IDisposable>(MockBehavior.Loose);
    var mockSonarApi = new Mock<ISonarClient>(MockBehavior.Loose);

    mockSonarApi
      .Setup(s => s.RecordHealthCheckDataAsync(
        It.IsAny<String>(),
        It.IsAny<String>(),
        It.IsAny<String>(),
        It.IsAny<ServiceHealthData>(),
        It.IsAny<CancellationToken>()))
      .Throws(ex);

    var reportingRunner = new ReportingMetricQueryRunner(
      mockRunner.Object,
      () => (mockDisposable.Object, mockSonarApi.Object),
      Mock.Of<ILogger<ReportingMetricQueryRunner>>()
    );

    var result = await reportingRunner.QueryRangeAsync(
      hcId,
      expr,
      start,
      end,
      CancellationToken.None
    );

    // Verify the inner IMetricQueryRunner is called with the original start and end date
    mockRunner.Verify(qr => qr.QueryRangeAsync(hcId, expr, start, end, CancellationToken.None));

    mockSonarApi.Verify(api => api.RecordHealthCheckDataAsync(
      hcId.Environment,
      hcId.Tenant,
      hcId.Service,
      It.Is<ServiceHealthData>(recorded => Equivalent(hcId.Name, data, recorded)),
      CancellationToken.None
    ));

    // The data should be returned as is
    Assert.NotNull(result);
    Assert.Equal(data, result);
  }

  // Task cancellation raises exception
  // Unexpected exception raised
  [Theory]
  [MemberData(nameof(UnhandledErrors))]
  public async Task Query_ReportMetrics_RaisedException(Exception ex, Boolean afterCancellation) {
    var hc = $"{Guid.NewGuid()}";
    var hcId = new HealthCheckIdentifier("env", "ten", "svc", hc);
    var expr = $"{Guid.NewGuid()}";

    var end = DateTime.UtcNow;
    var start = end.Subtract(TimeSpan.FromMinutes(10));
    var data = MockMetricQueryRunner.GenerateData(start, end).ToImmutableList();

    var mockRunner = MockMetricQueryRunner.CreateFixedReturnMock(data);
    var mockDisposable = new Mock<IDisposable>(MockBehavior.Loose);
    var mockSonarApi = new Mock<ISonarClient>(MockBehavior.Loose);

    mockSonarApi
      .Setup(s => s.RecordHealthCheckDataAsync(
        It.IsAny<String>(),
        It.IsAny<String>(),
        It.IsAny<String>(),
        It.IsAny<ServiceHealthData>(),
        It.IsAny<CancellationToken>()))
      .Throws(ex);

    var reportingRunner = new ReportingMetricQueryRunner(
      mockRunner.Object,
      () => (mockDisposable.Object, mockSonarApi.Object),
      Mock.Of<ILogger<ReportingMetricQueryRunner>>()
    );

    var tokenSource = new CancellationTokenSource();

    if (afterCancellation) {
      tokenSource.Cancel();
    }

    var raised = await Assert.ThrowsAnyAsync<Exception>(() => reportingRunner.QueryRangeAsync(
      hcId,
      expr,
      start,
      end,
      tokenSource.Token
    ));

    Assert.Same(ex, raised);
  }

  private static Boolean Equivalent(
    String healthCheck,
    ImmutableList<(DateTime, Decimal)> data,
    ServiceHealthData recorded) {

    return recorded.HealthCheckSamples.TryGetValue(healthCheck, out var recordedData) &&
      (data.Count == recordedData.Count) &&
      data.Zip(
          recordedData,
          (expected, actual) =>
            (expected.Item1 == actual.Timestamp) &&
            (Math.Abs((Double)expected.Item2 - actual.Value) <= Double.Epsilon))
        .All(x => x);
  }
}
