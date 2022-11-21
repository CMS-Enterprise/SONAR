using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Http;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Prometheus;

public class PrometheusClient : IPrometheusClient {
  private const String BaseUrlPath = "/api/v1";
  private const String QueryUrlPath = "/query";
  private const String QueryRangeUrlPath = "/query_range";
  private const String FormUrlEncodedMediaType = "application/x-www-form-urlencoded";

  private readonly HttpClient _client;
  private readonly JsonSerializerOptions _serializerOptions;

  private static readonly JsonSerializerOptions DefaultSerializerOptions = new() {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

  public PrometheusClient(HttpClient client, JsonSerializerOptions serializerOptions) {
    this._client = client;
    this._serializerOptions = serializerOptions;
  }

  public PrometheusClient(HttpClient client) : this(client, PrometheusClient.DefaultSerializerOptions) {

  }

  public async Task<ResponseEnvelope<QueryResults>> QueryAsync(
    String query,
    DateTime timestamp,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default) {

    var parameters = new QueryStringParameterCollection {
      { "query", query },
      { "time", timestamp },
    };

    if (timeout.HasValue) {
      parameters.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(timeout.Value));
    }

    var response = await this._client.GetAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryUrlPath}?{parameters}",
      cancellationToken
    );

    return await this.HandleQueryResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryPostAsync(
    QueryPostRequest request,
    CancellationToken cancellationToken = default) {

    var content = new QueryStringParameterCollection {
      { "query", request.Query },
      { "time", request.Timestamp },
    };
    if (request.Timeout.HasValue) {
      content.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(request.Timeout.Value));
    }

    var response = await this._client.PostAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryUrlPath}",
      new StringContent(
        content.ToString(),
        Encoding.UTF8,
        PrometheusClient.FormUrlEncodedMediaType),
      cancellationToken
    );

    return await this.HandleQueryResponse(response, cancellationToken);
  }

  private async Task<ResponseEnvelope<QueryResults>> HandleQueryResponse(
    HttpResponseMessage response, CancellationToken ct) {
    // if (successful response)
    //   Deserialize ResponseEnvelope<QueryResults>
    if (response.IsSuccessStatusCode) {
      var responseBody =
        await response.Content.ReadFromJsonAsync<ResponseEnvelope<QueryResults>>(_serializerOptions, ct);
      return responseBody ?? throw new InvalidOperationException("Prometheus API returned null response to query.");
    } else {
      // Non success error code? throw exception
      //   if there is a body w/ ErrorType/Error, include that in the exception message (TODO: special exception type?)
      if (response.Content.Headers.Contains("content-type")) {
        var responseBody =
          await response.Content.ReadFromJsonAsync<ResponseEnvelope<QueryResults>>(_serializerOptions, ct);
        throw new Exception(
          $"Prometheus returned non success status code ({response.StatusCode}) from query operation. Error Type: {responseBody?.ErrorType}, Detail: {responseBody?.Error}"
        );
      } else {
        throw new Exception(
          $"Prometheus returned non success status code ({response.StatusCode}) from query operation."
        );
      }
    }
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryRangeAsync(
    String query,
    DateTime start,
    DateTime end,
    TimeSpan step,
    TimeSpan? timeout,
    CancellationToken cancellationToken = default) {

    var parameters = new QueryStringParameterCollection {
      { "query", query },
      { "start", start },
      { "end", end },
      { "step", step.TotalSeconds },
    };

    if (timeout.HasValue) {
      parameters.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(timeout.Value));
    }

    var response = await this._client.GetAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryRangeUrlPath}?{parameters}",
      cancellationToken
    );

    return await this.HandleQueryResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryRangePostAsync(
    QueryRangePostRequest request,
    CancellationToken cancellationToken = default) {

    var content = new QueryStringParameterCollection {
      { "query", request.Query },
      { "start", request.Start },
      { "end", request.End },
      { "step", request.Step.TotalSeconds },
    };

    if (request.Timeout.HasValue) {
      content.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(request.Timeout.Value));
    }

    var response = await this._client.PostAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryRangeUrlPath}",
      new StringContent(
        content.ToString(),
        Encoding.UTF8,
        PrometheusClient.FormUrlEncodedMediaType),
      cancellationToken
    );

    return await this.HandleQueryResponse(response, cancellationToken);
  }

  /// <summary>
  ///   Converts a C# <see cref="TimeSpan" /> to a
  ///   <a href="https://prometheus.io/docs/prometheus/latest/querying/basics/#time-durations">
  ///     Prometheus duration string
  ///   </a>
  ///   consisting of integer numbers time units.
  /// </summary>
  /// <remarks>
  ///   The maximum supported unit in the output is days.
  /// </remarks>
  public static String ToPrometheusDuration(TimeSpan duration) {
    var parts = new List<String>();
    if (duration.Days > 0) {
      parts.Add($"{duration.Days}d");
    }
    if (duration.Hours > 0) {
      parts.Add($"{duration.Hours}h");
    }
    if (duration.Minutes > 0) {
      parts.Add($"{duration.Minutes}m");
    }
    if (duration.Seconds > 0) {
      parts.Add($"{duration.Seconds}s");
    }
    if (duration.Milliseconds > 0) {
      parts.Add($"{duration.Milliseconds}ms");
    }

    return String.Join(separator: " ", parts);
  }
}
