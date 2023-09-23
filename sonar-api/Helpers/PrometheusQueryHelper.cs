using System;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.Extensions.Logging;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;

namespace Cms.BatCave.Sonar.Helpers;

public class PrometheusQueryHelper {
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
}
