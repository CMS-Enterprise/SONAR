using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.Helpers;
using Cms.BatCave.Sonar.Agent.VersionChecks.Models;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.VersionChecks;

public class HttpResponseBodyVersionRequester : IVersionRequester<HttpResponseBodyVersionCheckDefinition> {

  private readonly HttpClient _httpClient;

  public HttpResponseBodyVersionRequester(HttpClient httpClient) {
    this._httpClient = httpClient;
  }

  public async Task<VersionResponse> GetVersionAsync(
    HttpResponseBodyVersionCheckDefinition versionCheckDefinition,
    CancellationToken ct = default) {

    var requestTimestamp = DateTime.UtcNow;

    var response = await this._httpClient.GetAsync(versionCheckDefinition.Url, ct);

    if (!response.IsSuccessStatusCode) {
      throw new VersionRequestException(
        $"Received non-success HTTP status from {versionCheckDefinition.Url}: " +
        $"{(Int32)response.StatusCode} {response.StatusCode}");
    }

    var responseBody = await response.Content.ReadAsStringAsync(ct);

    var version = DocumentValueExtractor.GetStringValue(
      versionCheckDefinition.BodyType,
      responseBody,
      versionCheckDefinition.Path);

    return new VersionResponse(requestTimestamp, version);
  }

}
