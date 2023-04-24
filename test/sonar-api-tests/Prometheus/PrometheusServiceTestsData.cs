using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cms.BatCave.Sonar.Query;
using ImmutableSamplesList = System.Collections.Immutable.IImmutableList<(System.DateTime Timestamp, System.Double Value)>;

namespace Cms.BatCave.Sonar.Tests.Prometheus;

public class PrometheusServiceTestsData {

  public const String Environment = "test-environment";
  public const String Tenant = "test-tenant";
  public const String Service = "test-service";

  public static DateTime SixtyOneMinutesAgo => DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(61));
  public static DateTime ElevenMinutesAgo => DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(11));
  public static DateTime TenMinutesAgo => DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
  public static DateTime FiveMinutesAgo => DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5));
  public static DateTime OneMinuteAgo => DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

  public static IImmutableDictionary<String, ImmutableSamplesList> CreateHealthCheckSamples(
    Int32 numHealthChecks = 2,
    Int32 numSamplesPerHealthCheck = 5,
    DateTime? finalSampleTimestamp = null,
    TimeSpan? timeBetweenSamples = null) {

    return Enumerable
      .Range(start: 1, numHealthChecks)
      .ToImmutableDictionary(
        keySelector: i => $"health-check-{i}",
        elementSelector: i => CreateSamples(numSamplesPerHealthCheck, finalSampleTimestamp, timeBetweenSamples));
  }

  public static IImmutableDictionary<String, ImmutableSamplesList> CreateHealthCheckSamples(
    Dictionary<String, ImmutableSamplesList> mutableHealthCheckSamples) {

    return mutableHealthCheckSamples.ToImmutableDictionary();
  }

  public static ImmutableSamplesList CreateSamples(
    Int32 numSamples,
    DateTime? finalSampleTimestamp = null,
    TimeSpan? timeBetweenSamples = null) {

    finalSampleTimestamp ??= DateTime.UtcNow;
    timeBetweenSamples ??= TimeSpan.FromSeconds(10);

    return Enumerable
      .Range(start: 0, numSamples)
      .Reverse()
      .Select(i => (finalSampleTimestamp.Value.Subtract(timeBetweenSamples.Value * i), (Double)numSamples - i))
      .ToImmutableList();
  }

  public static ImmutableSamplesList CreateSamples(
    List<(DateTime Timestamp, Double Value)> mutableSamples) {

    return mutableSamples.ToImmutableList();
  }

  public static ResponseEnvelope<QueryResults> CreateSuccessfulMatrixQueryResults(List<ResultData> resultData) =>
    CreateSuccessfulQueryResults(QueryResultType.Matrix, resultData);

  public static ResponseEnvelope<QueryResults> CreateSuccessfulQueryResults(
    QueryResultType resultType,
    List<ResultData> resultData) {

    return new ResponseEnvelope<QueryResults>(
      Status: ResponseStatus.Success,
      Data: new QueryResults(
        ResultType: resultType,
        Result: resultData.ToImmutableList(),
        Statistics: default),
      ErrorType: default,
      Error: default,
      Warnings: default);
  }

  public static ResultData CreateResultData(
    Dictionary<String, String> labels,
    (Decimal, String)? value = default,
    List<(Decimal, String)>? values = default) {
    return new ResultData(
      Labels: labels.ToImmutableDictionary(),
      Value: value,
      Values: values?.ToImmutableList());
  }

  public static ResponseEnvelope<QueryResults> CreateUnsuccessfulQueryResults() =>
    new(
      Status: ResponseStatus.Error,
      ErrorType: "test-error-type",
      Error: "test-error",
      Warnings: new[] { "test-warning" },
      Data: null);
}
