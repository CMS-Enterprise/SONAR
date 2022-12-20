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
using Cms.BatCave.Sonar.Query;

namespace Cms.BatCave.Sonar.Loki;

public class LokiClient : ILokiClient {
  private const String BaseUrlPath = "loki/api/v1";
  private const String QueryUrlPath = "/query";
  private const String QueryRangeUrlPath = "/query_range";

  private readonly HttpClient _client;
  private readonly JsonSerializerOptions _serializerOptions;

  private static readonly JsonSerializerOptions DefaultSerializerOptions = new() {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

  public LokiClient(HttpClient client, JsonSerializerOptions serializerOptions) {
    this._client = client;
    this._serializerOptions = serializerOptions;
  }

  public LokiClient(HttpClient client) : this(client, LokiClient.DefaultSerializerOptions) {

  }

  public async Task<ResponseEnvelope<QueryResults>> QueryAsync(
    String query,
    Int32 limit,
    DateTime timestamp,
    Direction direction,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection {
      { "query", query },
      { "limit", limit },
      { "time", timestamp },
      { "direction", direction }
    };

    var response = await this._client.GetAsync(
      $"{LokiClient.BaseUrlPath}{LokiClient.QueryUrlPath}?{parameters}",
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
      return responseBody ?? throw new InvalidOperationException("Loki API returned null response to query.");
    } else {
      // Non success error code? throw exception
      //   if there is a body w/ ErrorType/Error, include that in the exception message (TODO: special exception type?)
      if (response.Content.Headers.Contains("content-type")) {
        var responseBody =
          await response.Content.ReadAsStringAsync(ct);
        throw new Exception(
          $"Loki returned non success status code ({response.StatusCode}) from query operation. Error Message: {responseBody}"
        );
      } else {
        throw new Exception(
          $"Loki returned non success status code ({response.StatusCode}) from query operation."
        );
      }
    }
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryRangeAsync(
    String query,
    DateTime start,
    DateTime end,
    Int32? limit = default,
    TimeSpan? step = default,
    Direction? direction = default,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection {
      { "query", query },
      { "start", start },
      { "end", end }
    };

    if (limit.HasValue) {
      parameters.Add("limit", limit.Value);
    }

    if (step.HasValue) {
      parameters.Add("step", step.Value);
    }

    if (direction.HasValue) {
      parameters.Add("direction", direction.Value.ToString().ToLowerInvariant());
    }

    var response = await this._client.GetAsync(
      $"{LokiClient.BaseUrlPath}{LokiClient.QueryRangeUrlPath}?{parameters}",
      cancellationToken
    );

    return await this.HandleQueryResponse(response, cancellationToken);
  }
}
