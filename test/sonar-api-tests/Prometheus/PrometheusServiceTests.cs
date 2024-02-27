using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Prometheus;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using Prometheus;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;
using Xunit;
using TestData = Cms.BatCave.Sonar.Tests.Prometheus.PrometheusServiceTestsData;
using ImmutableSamplesList = System.Collections.Immutable.IImmutableList<(System.DateTime Timestamp, System.Double Value)>;

namespace Cms.BatCave.Sonar.Tests.Prometheus;

public class PrometheusServiceTests {

  private readonly PrometheusService _prometheusService;

  private Mock<IPrometheusRemoteProtocolClient> MockPrometheusRemoteProtocolClient { get; } = new();
  private Mock<IPrometheusClient> MockPrometheusClient { get; } = new();
  private Mock<ILogger<PrometheusService>> MockLogger { get; } = new();

  public PrometheusServiceTests() {
    this._prometheusService = new PrometheusService(
      this.MockLogger.Object,
      this.MockPrometheusRemoteProtocolClient.Object,
      this.MockPrometheusClient.Object);
  }

  #region WriteHealthCheckDataAsync Tests

  [Fact]
  public async Task WriteHealthCheckDataAsync_AllSamplesAreFresh_AllSamplesAreWrittenToPrometheus() {
    var testInputData = new ServiceHealthData(TestData.CreateHealthCheckSamples());

    // For this test, we want IPrometheusClient#QueryAsync to behave like Prometheus has no data, so filtering on the
    // basis of input samples being older than already-recorded samples won't occur.
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData>());

    var capturedWriteRequests = new List<WriteRequest>();

    this.MockPrometheusRemoteProtocolClient
      .Setup(client =>
        client.WriteAsync(
          Capture.In(capturedWriteRequests),
          It.IsAny<CancellationToken>()));

    await this._prometheusService.WriteHealthCheckDataAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      testInputData);

    // Implicitly asserts there's only one WriteRequest, from one call to IPrometheusRemoteProtocolClient#WriteAsync.
    var writeRequest = capturedWriteRequests.Single();
    // Assert there are as many time series written as there are health checks in the input, and that each
    // written time series has the same number of samples as it's corresponding input.
    Assert.Equal(testInputData.HealthCheckSamples.Count, writeRequest.Timeseries.Count);
    foreach (var (kvp, i) in testInputData.HealthCheckSamples.Select((kvp, i) => (kvp, i))) {
      Assert.Equal(kvp.Value.Count, writeRequest.Timeseries[i].Samples.Count);
    }
  }

  [Fact]
  public async Task WriteHealthCheckDataAsync_AllSamplesAreStale_PrometheusIsNotCalled() {
    var testInputData = new ServiceHealthData(
      TestData.CreateHealthCheckSamples(new Dictionary<String, ImmutableSamplesList> {
        // Should always be considered stale.
        ["health-check-1"] = TestData.CreateSamples(5, TestData.SixtyOneMinutesAgo),
        // Should be considered stale if older than the latest already-recorded data.
        ["health-check-2"] = TestData.CreateSamples(5, TestData.TenMinutesAgo)
      }));

    // For this test, we want IPrometheusClient#QueryAsync to behave like Prometheus already has data for health-check-2
    // from 11 minutes ago and one minute ago. In this case, PrometheusService#WriteHealthCheckDataAsync should filter
    // health-check-2 samples that are older than the most recent samples Prometheus already has, one minute ago.
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData> {
      TestData.CreateResultData(
        labels: new Dictionary<String, String> {
          [HealthDataHelper.MetricLabelKeys.HealthCheck] = "health-check-2"
        },
        values: new List<(Decimal, String)> {
          ((Decimal)TestData.OneMinuteAgo.SecondsSinceUnixEpoch(), "1.0"),
          ((Decimal)TestData.ElevenMinutesAgo.SecondsSinceUnixEpoch(), "1.0")
        })
    });

    await this._prometheusService.WriteHealthCheckDataAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      testInputData);

    // In this case, all input data should be stale, leaving nothing to write to Prometheus; consequently,
    // IPrometheusRemoteProtocolClient#WriteAsync should not be called.
    this.MockPrometheusRemoteProtocolClient
      .Verify(
        expression: client =>
          client.WriteAsync(
            It.IsAny<WriteRequest>(),
            It.IsAny<CancellationToken>()),
        times: Times.Never());
  }

  [Fact]
  public async Task WriteHealthCheckDataAsync_MixOfFreshAndStaleSamples_OnlyFreshSamplesAreWritten() {
    var testInputData = new ServiceHealthData(
      TestData.CreateHealthCheckSamples(new Dictionary<String, ImmutableSamplesList> {
        ["health-check-1"] = TestData.CreateSamples(new List<(DateTime Timestamp, Double Value)> {
          // Should always be considered stale.
          (TestData.SixtyOneMinutesAgo, 1.0),
          // Should be considered stale for this test.
          (TestData.TenMinutesAgo, 1.0),
          // Should be considered fresh for this test.
          (TestData.OneMinuteAgo, 1.0)
        })
      }));

    // For this test, we want IPrometheusClient#QueryAsync to behave like Prometheus already has data for health-check-1
    // from 5 minutes ago. In this case, PrometheusService#WriteHealthCheckDataAsync should filter health-check-1
    // samples that are older than 5 minutes.
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData> {
      TestData.CreateResultData(
        labels: new Dictionary<String, String> {
          [HealthDataHelper.MetricLabelKeys.HealthCheck] = "health-check-1"
        },
        values: new List<(Decimal, String)> {
          ((Decimal)TestData.FiveMinutesAgo.SecondsSinceUnixEpoch(), "1.0")
        })
    });

    var capturedWriteRequests = new List<WriteRequest>();

    this.MockPrometheusRemoteProtocolClient
      .Setup(client =>
        client.WriteAsync(
          Capture.In(capturedWriteRequests),
          It.IsAny<CancellationToken>()));

    await this._prometheusService.WriteHealthCheckDataAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      testInputData);

    // Implicitly asserts there's only one WriteRequest, from one call to IPrometheusRemoteProtocolClient#WriteAsync.
    var writeRequest = capturedWriteRequests.Single();
    // Assert there is one time series written, with one sample.
    Assert.Single(writeRequest.Timeseries);
    Assert.Single(writeRequest.Timeseries[0].Samples);
  }

  [Fact]
  public async Task WriteHealthCheckDataAsync_WrittenSamplesAreReturnedToCaller() {
    var random = new Random();
    var testInputData = new ServiceHealthData(TestData.CreateHealthCheckSamples(
      numHealthChecks: random.Next(minValue: 10, maxValue: 51),
      numSamplesPerHealthCheck: random.Next(minValue: 1, maxValue: 11)));

    // For this test, we want IPrometheusClient#QueryAsync to behave like Prometheus has no data, so filtering on the
    // basis of input samples being older than already-recorded samples won't occur.
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData>());

    var writtenData = await this._prometheusService.WriteHealthCheckDataAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      testInputData);

    Assert.Equal(testInputData.TotalHealthChecks, writtenData.TotalHealthChecks);
    Assert.Equal(testInputData.TotalSamples, writtenData.TotalSamples);
  }

  [Fact]
  public async Task WriteHealthCheckDataAsync_WrittenTimeseriesHaveExpectedLabels() {
    var random = new Random();
    var testInputData = new ServiceHealthData(TestData.CreateHealthCheckSamples(
      numHealthChecks: random.Next(minValue: 10, maxValue: 51),
      numSamplesPerHealthCheck: random.Next(minValue: 1, maxValue: 11)));

    // For this test, we want IPrometheusClient#QueryAsync to behave like Prometheus has no data, so filtering on the
    // basis of input samples being older than already-recorded samples won't occur.
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData>());

    var capturedWriteRequests = new List<WriteRequest>();

    this.MockPrometheusRemoteProtocolClient
      .Setup(client =>
        client.WriteAsync(
          Capture.In(capturedWriteRequests),
          It.IsAny<CancellationToken>()));

    await this._prometheusService.WriteHealthCheckDataAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      testInputData);

    // Implicitly asserts there's only one WriteRequest, from one call to IPrometheusRemoteProtocolClient#WriteAsync.
    var writeRequest = capturedWriteRequests.Single();
    // Assert there are as many time series written as there are health checks in the input, and that each
    // written time series has all of (and only) the expected labels.
    Assert.Equal(testInputData.HealthCheckSamples.Count, writeRequest.Timeseries.Count);
    foreach (var (kvp, i) in testInputData.HealthCheckSamples.Select((kvp, i) => (kvp, i))) {
      Assert.Collection(
        writeRequest.Timeseries[i].Labels,
        label => {
          Assert.Equal("__name__", label.Name);
          Assert.Equal(HealthDataHelper.ServiceHealthCheckDataMetricName, label.Value);
        },
        label => {
          Assert.Equal(HealthDataHelper.MetricLabelKeys.Environment, label.Name);
          Assert.Equal(TestData.Environment, label.Value);
        },
        label => {
          Assert.Equal(HealthDataHelper.MetricLabelKeys.Tenant, label.Name);
          Assert.Equal(TestData.Tenant, label.Value);
        },
        label => {
          Assert.Equal(HealthDataHelper.MetricLabelKeys.Service, label.Name);
          Assert.Equal(TestData.Service, label.Value);
        },
        label => {
          Assert.Equal(HealthDataHelper.MetricLabelKeys.HealthCheck, label.Name);
          Assert.Equal(kvp.Key, label.Value);
        });
    }
  }

  [Fact]
  public async Task WriteHealthCheckDataAsync_WriteRequestHasExpectedMetadata() {
    var testInputData = new ServiceHealthData(TestData.CreateHealthCheckSamples(numHealthChecks: 1));

    // For this test, we want IPrometheusClient#QueryAsync to behave like Prometheus has no data, so filtering on the
    // basis of input samples being older than already-recorded samples won't occur.
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData>());

    var capturedWriteRequests = new List<WriteRequest>();

    this.MockPrometheusRemoteProtocolClient
      .Setup(client =>
        client.WriteAsync(
          Capture.In(capturedWriteRequests),
          It.IsAny<CancellationToken>()));

    await this._prometheusService.WriteHealthCheckDataAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      testInputData);

    // Implicitly asserts there's only one WriteRequest, from one call to IPrometheusRemoteProtocolClient#WriteAsync.
    var writeRequest = capturedWriteRequests.Single();
    // Assert the write request has one MetricMetadata, and that it's fields have expected values.
    Assert.Single(writeRequest.Metadata);
    Assert.NotEmpty(writeRequest.Metadata[0].Help);
    Assert.Equal(MetricMetadata.Types.MetricType.Gauge, writeRequest.Metadata[0].Type);
    Assert.Equal(HealthDataHelper.ServiceHealthCheckDataMetricName, writeRequest.Metadata[0].MetricFamilyName);
  }

  #endregion WriteHealthCheckDataAsync Tests

  #region QueryLatestHealthCheckDataTimestampsAsync Tests

  [Theory]
  [InlineData(61 * 60 + 10, "1h1m10s")]
  [InlineData(30 * 60, "30m")]
  public async Task QueryLatestHealthCheckDataTimestampsAsync_ExpectedQueryIsExecuted(
    Int32 queryTimeRangeSeconds,
    String expectedPrometheusQueryTimeRange) {

    List<String> capturedQueries = new();

    this.MockPrometheusClient
      .Setup(client =>
        client.QueryAsync(
          Capture.In(capturedQueries),
          It.IsAny<DateTime>(),
          It.IsAny<TimeSpan?>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(TestData.CreateSuccessfulMatrixQueryResults(new List<ResultData>()));

    await this._prometheusService.QueryLatestHealthCheckDataTimestampsAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      TimeSpan.FromSeconds(queryTimeRangeSeconds));

    var expectedQuery = String.Format(
      format: "{0}{{environment='{1}',tenant='{2}',service='{3}'}}[{4}]",
      HealthDataHelper.ServiceHealthCheckDataMetricName,
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      expectedPrometheusQueryTimeRange);

    // Implicitly asserts there's only one query, from one call to IPrometheusClient#QueryAsync.
    var query = capturedQueries.Single();
    Assert.Equal(expectedQuery, query);
  }

  [Fact]
  public async Task QueryLatestHealthCheckDataTimestampsAsync_CorrectTimestampsAreReturnedFromQueryResponse() {
    // Record these dynamic values once, so we can use them later in assertions.
    var sixtyOneMinutesAgo = TestData.SixtyOneMinutesAgo;
    var elevenMinutesAgo = TestData.ElevenMinutesAgo;
    var tenMinutesAgo = TestData.TenMinutesAgo;
    var fiveMinutesAgo = TestData.FiveMinutesAgo;
    var oneMinuteAgo = TestData.OneMinuteAgo;

    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData> {
      TestData.CreateResultData(
        labels: new Dictionary<String, String> {
          [HealthDataHelper.MetricLabelKeys.HealthCheck] = "health-check-1"
        },
        values: new List<(Decimal, String)> {
          ((Decimal)tenMinutesAgo.SecondsSinceUnixEpoch(), "1.0"),
          ((Decimal)sixtyOneMinutesAgo.SecondsSinceUnixEpoch(), "1.0")
        }),
      TestData.CreateResultData(
        labels: new Dictionary<String, String> {
          [HealthDataHelper.MetricLabelKeys.HealthCheck] = "health-check-2"
        },
        values: new List<(Decimal, String)> {
          ((Decimal)elevenMinutesAgo.SecondsSinceUnixEpoch(), "1.0"),
          ((Decimal)oneMinuteAgo.SecondsSinceUnixEpoch(), "1.0"),
          ((Decimal)fiveMinutesAgo.SecondsSinceUnixEpoch(), "1.0")
        }),
    });

    var latestHealthCheckDataTimestamps = await this._prometheusService.QueryLatestHealthCheckDataTimestampsAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      TimeSpan.FromMinutes(90));

    // Assert that there are exactly two entries in the returned result, and that they have the expected health check
    // names and timestamps. Because of differences of time formats used internally by our API and how
    // Prometheus expresses times, we allow a millisecond fudge-factor in our timestamp comparisons.
    Assert.Equal(expected: 2, latestHealthCheckDataTimestamps.Count);
    Assert.Contains(expected: "health-check-1", latestHealthCheckDataTimestamps);
    Assert.Equal(
      tenMinutesAgo.MillisSinceUnixEpoch(),
      latestHealthCheckDataTimestamps["health-check-1"].MillisSinceUnixEpoch(),
      tolerance: 1.0);
    Assert.Contains(expected: "health-check-2", latestHealthCheckDataTimestamps);
    Assert.Equal(
      oneMinuteAgo.MillisSinceUnixEpoch(),
      latestHealthCheckDataTimestamps["health-check-2"].MillisSinceUnixEpoch(),
      tolerance: 1.0);
  }

  [Fact]
  public async Task QueryLatestHealthCheckDataTimestampsAsync_QueryResponseContainsNoData_EmptyDictionaryIsReturned() {
    this.MockPrometheusClient
      .Setup(client => client.QueryAsync(
        It.IsAny<String>(),
        It.IsAny<DateTime>(),
        It.IsAny<TimeSpan?>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(TestData.CreateUnsuccessfulQueryResults());

    var latestHealthCheckDataTimestamps = await this._prometheusService.QueryLatestHealthCheckDataTimestampsAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      TimeSpan.FromSeconds(0));

    Assert.Empty(latestHealthCheckDataTimestamps);
  }

  [Fact]
  public async Task QueryLatestHealthCheckDataTimestampsAsync_QueryResponseDataContainsNoValues_UnixEpochIsReturned() {
    this.MockPrometheusClient.SetupSuccessfulQueryAsyncReturns(new List<ResultData> {
      TestData.CreateResultData(
        labels: new Dictionary<String, String> {
          [HealthDataHelper.MetricLabelKeys.HealthCheck] = "health-check-1"
        },
        values: null)
    });

    var latestHealthCheckDataTimestamps = await this._prometheusService.QueryLatestHealthCheckDataTimestampsAsync(
      TestData.Environment,
      TestData.Tenant,
      TestData.Service,
      TimeSpan.FromSeconds(0));

    Assert.Contains(expected: "health-check-1", latestHealthCheckDataTimestamps);
    Assert.Equal(DateTime.UnixEpoch, latestHealthCheckDataTimestamps["health-check-1"]);
  }

  #endregion QueryLatestHealthCheckDataTimestampsAsync Tests

  #region GetAlertmanagerScrapeStatusAsync Tests

  private static QueryResults AlertmanagerHealthCheckQueryResults =>
    new(
      ResultType: QueryResultType.Vector,
      Result: ImmutableList<ResultData>.Empty,
      Statistics: null);

  private static ResponseEnvelope<QueryResults> AlertmanagerHealthCheckResponseEnvelope =>
    new(
      Status: ResponseStatus.Success,
      Data: AlertmanagerHealthCheckQueryResults,
      ErrorType: null,
      Error: null,
      Warnings: null);

  [Fact]
  public async Task GetAlertmanagerScrapeStatusAsync_ErrorResponseFromPrometheusQuery_ReturnsUnknown() {
    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerBuildInfoTimestamp()
      .ReturnsAsync(AlertmanagerHealthCheckResponseEnvelope with {
        Status = ResponseStatus.Error
      })
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerScrapeStatusAsync(default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Unknown, actual: status);
  }

  [Theory]
  [MemberData(nameof(GetAlertmanagerScrapeStatusAsync_EmptyResponseFromPrometheusQuery_ReturnsOffline_Data))]
  public async Task GetAlertmanagerScrapeStatusAsync_EmptyResponseFromPrometheusQuery_ReturnsOffline(
    ResponseEnvelope<QueryResults> responseEnvelope) {

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerBuildInfoTimestamp()
      .ReturnsAsync(responseEnvelope)
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerScrapeStatusAsync(default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Offline, actual: status);
  }

  public static IEnumerable<Object[]>
    GetAlertmanagerScrapeStatusAsync_EmptyResponseFromPrometheusQuery_ReturnsOffline_Data =>
    new List<Object[]> {
      new Object[] { AlertmanagerHealthCheckResponseEnvelope },
      new Object[] { AlertmanagerHealthCheckResponseEnvelope with { Data = null } }
    };

  [Fact]
  public async Task GetAlertmanagerScrapeStatusAsync_BuildInfoMetricTooOld_ReturnsDegraded() {
    var staleTimeSpan = IPrometheusService.AlertmanagerScrapeInterval + TimeSpan.FromSeconds(61);
    var staleSample = new ResultData(
      Labels: ImmutableDictionary<String, String>.Empty,
      Value: (
        Convert.ToDecimal(DateTime.UtcNow.SecondsSinceUnixEpoch()),
        DateTime.UtcNow.Subtract(staleTimeSpan).SecondsSinceUnixEpoch().ToString(CultureInfo.InvariantCulture)
      ),
      Values: null);

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerBuildInfoTimestamp()
      .ReturnsAsync(AlertmanagerHealthCheckResponseEnvelope with {
        Data = AlertmanagerHealthCheckQueryResults with {
          Result = ImmutableList.Create(staleSample)
        }
      })
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerScrapeStatusAsync(default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Degraded, actual: status);
  }

  [Fact]
  public async Task GetAlertmanagerScrapeStatusAsync_BuildInfoMetricUpToDate_ReturnsOnline() {
    var freshTimeSpan = IPrometheusService.AlertmanagerScrapeInterval;
    var freshSample = new ResultData(
      Labels: ImmutableDictionary<String, String>.Empty,
      Value: (
        Convert.ToDecimal(DateTime.UtcNow.SecondsSinceUnixEpoch()),
        DateTime.UtcNow.Subtract(freshTimeSpan).SecondsSinceUnixEpoch().ToString(CultureInfo.InvariantCulture)
      ),
      Values: null);

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerBuildInfoTimestamp()
      .ReturnsAsync(AlertmanagerHealthCheckResponseEnvelope with {
        Data = AlertmanagerHealthCheckQueryResults with {
          Result = ImmutableList.Create(freshSample)
        }
      })
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerScrapeStatusAsync(default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Online, actual: status);
  }

  [Theory]
  [MemberData(nameof(AnyException_Data))]
  public async Task GetAlertmanagerScrapeStatusAsync_AnyException_ReturnsUnknown(
    Exception anyException) {

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerBuildInfoTimestamp()
      .ThrowsAsync(anyException)
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerScrapeStatusAsync(default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Unknown, actual: status);
  }

  public static IEnumerable<Object[]> AnyException_Data =>
    new List<Object[]> {
      new Object[] { new Exception() },
      new Object[] { new HttpRequestException() },
      new Object[] { new OperationCanceledException() }
    };

  #endregion GetAlertmanagerScrapeStatusAsync Tests

  #region GetAlertmanagerNotificationsStatusAsync Tests

  [Fact]
  public async Task GetAlertmanagerNotificationsStatusAsync_ErrorResponseFromPrometheusQuery_ReturnsUnknown() {
    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerNotificationsFailedMetric(integration: "email")
      .ReturnsAsync(AlertmanagerHealthCheckResponseEnvelope with {
        Status = ResponseStatus.Error
      })
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerNotificationsStatusAsync(
      integration: "email",
      cancellationToken: default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Unknown, actual: status);
  }

  [Fact]
  public async Task GetAlertmanagerNotificationsStatusAsync_NotificationFailuresIncreased_ReturnsDegraded() {
    var notificationFailuresIncreasedSample = new ResultData(
      Labels: ImmutableDictionary<String, String>.Empty,
      Value: (Convert.ToDecimal(DateTime.UtcNow.SecondsSinceUnixEpoch()), "1.0"),
      Values: null);

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerNotificationsFailedMetric(integration: "email")
      .ReturnsAsync(AlertmanagerHealthCheckResponseEnvelope with {
        Data = AlertmanagerHealthCheckQueryResults with {
          Result = ImmutableList.Create(notificationFailuresIncreasedSample)
        }
      })
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerNotificationsStatusAsync(
      integration: "email",
      cancellationToken: default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Degraded, actual: status);
  }

  [Fact]
  public async Task GetAlertmanagerNotificationsStatusAsync_NotificationFailuresNotIncreased_ReturnsOnline() {
    var notificationFailuresNotIncreasedSample = new ResultData(
      Labels: ImmutableDictionary<String, String>.Empty,
      Value: (Convert.ToDecimal(DateTime.UtcNow.SecondsSinceUnixEpoch()), "0.0"),
      Values: null);

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerNotificationsFailedMetric(integration: "email")
      .ReturnsAsync(AlertmanagerHealthCheckResponseEnvelope with {
        Data = AlertmanagerHealthCheckQueryResults with {
          Result = ImmutableList.Create(notificationFailuresNotIncreasedSample)
        }
      })
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerNotificationsStatusAsync(
      integration: "email",
      cancellationToken: default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Online, actual: status);
  }

  [Theory]
  [MemberData(nameof(GetAlertmanagerNotificationsStatusAsync_NoFailureData_ReturnsOnline_Data))]
  public async Task GetAlertmanagerNotificationsStatusAsync_NoFailureData_ReturnsOnline(
    ResponseEnvelope<QueryResults> responseEnvelope) {

    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerNotificationsFailedMetric(integration: "email")
      .ReturnsAsync(responseEnvelope)
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerNotificationsStatusAsync(
      integration: "email",
      cancellationToken: default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Online, actual: status);
  }

  public static IEnumerable<Object[]> GetAlertmanagerNotificationsStatusAsync_NoFailureData_ReturnsOnline_Data =>
    new List<Object[]> {
      new Object[] { AlertmanagerHealthCheckResponseEnvelope },
      new Object[] { AlertmanagerHealthCheckResponseEnvelope with { Data = null } }
    };

  [Theory]
  [MemberData(nameof(AnyException_Data))]
  public async Task GetAlertmanagerNotificationsStatusAsync_AnyException_ReturnsUnknown(
    Exception anyException) {
    this.MockPrometheusClient
      .SetupQueryAsyncForAlertmanagerNotificationsFailedMetric(integration: "email")
      .ThrowsAsync(anyException)
      .Verifiable();

    var status = await this._prometheusService.GetAlertmanagerNotificationsStatusAsync(
      integration: "email",
      cancellationToken: default);

    this.MockPrometheusClient.Verify();
    Assert.Equal(expected: HealthStatus.Unknown, actual: status);
  }

  #endregion GetAlertmanagerNotificationsStatusAsync Tests

}

internal static class MockPrometheusClientExtensions {
  /// <summary>
  /// A convenience method that sets up a <c>Mock&lt;IPrometheusClient&gt;</c>'s <see cref="IPrometheusClient.QueryAsync"/>
  /// method to return the given query result.
  /// </summary>
  /// <param name="mockPrometheusClient">The mock to apply the setup to.</param>
  /// <param name="resultData">The list of <see cref="ResultData"/> for the mock to return.</param>
  /// <returns>The <see cref="IReturnsResult{TMock}"/> for continued setup if desired.</returns>
  public static IReturnsResult<IPrometheusClient> SetupSuccessfulQueryAsyncReturns(
    this Mock<IPrometheusClient> mockPrometheusClient,
    List<ResultData> resultData) {

    return mockPrometheusClient
      .Setup(client =>
        client.QueryAsync(
          It.IsAny<String>(),
          It.IsAny<DateTime>(),
          It.IsAny<TimeSpan?>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        TestData.CreateSuccessfulMatrixQueryResults(resultData));
  }

  public static ISetup<IPrometheusClient, Task<ResponseEnvelope<QueryResults>>>
    SetupQueryAsyncForAlertmanagerBuildInfoTimestamp(this Mock<IPrometheusClient> mockPrometheusClient) {

    return mockPrometheusClient
      .Setup(client =>
        client.QueryAsync(
          "timestamp(alertmanager_build_info)",
          It.IsAny<DateTime>(),
          It.IsAny<TimeSpan?>(),
          It.IsAny<CancellationToken>()));
  }

  public static ISetup<IPrometheusClient, Task<ResponseEnvelope<QueryResults>>>
    SetupQueryAsyncForAlertmanagerNotificationsFailedMetric(
      this Mock<IPrometheusClient> mockPrometheusClient,
      String integration) {

    var metricQueryStringRegex =
      $"sum\\(increase\\(alertmanager_notification_requests_failed_total\\{{integration='{integration}'}}\\[.*\\]\\)\\)";

    return mockPrometheusClient
      .Setup(client =>
        client.QueryAsync(
          It.IsRegex(metricQueryStringRegex),
          It.IsAny<DateTime>(),
          It.IsAny<TimeSpan?>(),
          It.IsAny<CancellationToken>()));
  }
}
