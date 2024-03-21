using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.Extensions.Logging;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;

namespace Cms.BatCave.Sonar.Helpers;

public class PrometheusQueryHelper {
  public static readonly TimeSpan QueryRangeMaximumNumberDays = TimeSpan.FromDays(7);
  private readonly IPrometheusClient _prometheusClient;
  private readonly ILogger<PrometheusQueryHelper> _logger;

  public PrometheusQueryHelper(
    IPrometheusClient prometheusClient,
    ILogger<PrometheusQueryHelper> logger) {
    this._prometheusClient = prometheusClient;
    this._logger = logger;
  }

  public async Task<T> GetLatestValuePrometheusQuery<T>(
    String promQuery,
    TimeSpan maximumAge,
    Func<QueryResults, T> processResult,
    CancellationToken cancellationToken) {

    var response = await this._prometheusClient.QueryAsync(
      // metric{tag="value"}[time_window]
      $"{promQuery}[{PrometheusClient.ToPrometheusDuration(maximumAge)}]",
      DateTime.UtcNow,
      cancellationToken: cancellationToken
    );

    if (response.Status != ResponseStatus.Success) {
      this._logger.LogError(
        message: "Unexpected error querying data from Prometheus ({ErrorType}): {ErrorMessage}",
        response.ErrorType,
        response.Error
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying data."
      );
    }

    if (response.Data == null) {
      this._logger.LogError(
        message: "Prometheus unexpectedly returned null data for query {Query}",
        promQuery
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying data."
      );
    }

    return processResult(response.Data);
  }

  public async Task<T> GetInstantaneousValuePromQuery<T>(
    String promQuery,
    DateTime timeQuery,
    Func<QueryResults, T> processResult,
    CancellationToken cancellationToken) {

    var response = await this._prometheusClient.QueryAsync(
      // query={value}?time=timestamp
      $"{promQuery}",
      timeQuery,
      cancellationToken: cancellationToken
    );

    if (response.Status != ResponseStatus.Success) {
      this._logger.LogError(
        message: "Unexpected error querying data from Prometheus ({ErrorType}): {ErrorMessage}",
        response.ErrorType,
        response.Error
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying data."
      );
    }

    if (response.Data == null) {
      this._logger.LogError(
        message: "Prometheus unexpectedly returned null data for query {Query}",
        promQuery
      );
      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: "Error querying data."
      );
    }

    return processResult(response.Data);
  }

  public async Task<T> GetPrometheusQueryRangeValue<T>(
    String queryTopic,
    String promQuery,
    DateTime start,
    DateTime end,
    TimeSpan step,
    Func<QueryResults, T> processResult,
    CancellationToken cancellationToken) {

    try {
      var response = await this._prometheusClient.QueryRangeAsync(
        $"{promQuery}",
        start.ToUniversalTime(),
        end.ToUniversalTime(),
        step,
        cancellationToken: cancellationToken
      );

      if (response.Status != ResponseStatus.Success) {
        this._logger.LogError(
          message: "Unexpected error querying {queryTopic} from Prometheus ({ErrorType}): {ErrorMessage}",
        queryTopic,
          response.ErrorType,
          response.Error
        );
        throw new InternalServerErrorException(
          errorType: "PrometheusApiError",
          message: $"Error querying {queryTopic}."
        );
      }

      if (response.Data == null) {
        this._logger.LogError(
          message: "Prometheus unexpectedly returned null data for query {Query}",
          promQuery
        );
        throw new InternalServerErrorException(
          errorType: "PrometheusApiError",
          message: $"Error querying {queryTopic}."
        );
      }
      return processResult(response.Data);
    } catch (Exception e) {

      throw new InternalServerErrorException(
        errorType: "PrometheusApiError",
        message: $"Error querying {queryTopic} history. {e.Message}"
      );
    }
  }

  public (DateTime, DateTime, Int32) ValidateParameters(
    DateTime? queryStart,
    DateTime? queryEnd,
    Int32? queryStep) {

    var end = queryEnd?.ToUniversalTime() ?? DateTime.UtcNow;
    var start = queryStart?.ToUniversalTime() ?? end.Subtract(TimeSpan.FromHours(1));
    var step = queryStep ?? 30;

    var dataPoints = (end - start).TotalSeconds / step;
    if (dataPoints > 100) {
      throw new BadRequestException($"The number of data points (range in seconds / step in seconds) " +
        $"in the returned time series must be less than or equal to 100.");
    }

    if (end <= start) {
      throw new BadRequestException("End date cannot be earlier or equal to the start date");
    }

    // End - Start cannot be greater than 7 days to be consistent with Metric history restriction.
    if ((end - start) > TimeSpan.FromDays(7)) {
      throw new BadRequestException(
        $"The number of days must be less than or equal to {TimeSpan.FromDays(7).Days}"
      );
    }

    return (start, end, step);
  }
}
