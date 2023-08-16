using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Agent;

public partial class SonarClient {

  private readonly String _apiKeyValue;
  public SonarClient(IOptions<ApiConfiguration> apiConfig, HttpClient client) : this(apiConfig.Value.BaseUrl, client) {
    var apiKeyValue = apiConfig.Value.ApiKey;

    if (apiConfig.Value.ApiKeyId.HasValue) {
      apiKeyValue = apiConfig.Value.ApiKeyId + ":" + apiConfig.Value.ApiKey;
    }

    if (apiKeyValue != null) {
      this._apiKeyValue = apiKeyValue;
    }
  }

  partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings) {
    settings.PropertyNameCaseInsensitive = true;
    settings.Converters.Add(new JsonStringEnumConverter());
    settings.Converters.Add(new ArrayTupleConverterFactory());
  }

  partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url) {
    if (!request.Headers.Contains("ApiKey")) {
      request.Headers.Add("ApiKey", this._apiKeyValue);
    }
  }

  partial void PrepareRequest(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder) {
    if (!request.Headers.Contains("ApiKey")) {
      request.Headers.Add("ApiKey", this._apiKeyValue);
    }
  }
}
