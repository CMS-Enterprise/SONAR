using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Json;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Agent;

public partial class SonarClient {

  private readonly String _apiKeyValue;
  public SonarClient(IConfigurationRoot configuration, String baseUrl, HttpClient client) : this(baseUrl, client) {
    var apiKeyValue = configuration.GetSection("ApiKey").Value;
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
