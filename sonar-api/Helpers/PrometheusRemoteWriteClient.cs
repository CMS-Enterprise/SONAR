using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using Snappy;

namespace Cms.BatCave.Sonar.Helpers;

public class PrometheusRemoteWriteClient {
  private readonly Uri _prometheusUrl;
  private readonly ILogger<PrometheusRemoteWriteClient> _logger;

  public PrometheusRemoteWriteClient(
    IOptions<PrometheusConfiguration> prometheusConfig, ILogger<PrometheusRemoteWriteClient> logger) {
    this._prometheusUrl =
      new Uri(
        $"{prometheusConfig.Value.Protocol}://{prometheusConfig.Value.Host}:{prometheusConfig.Value.Port}/api/v1/write"
      );
    this._logger = logger;
  }

  public async Task<ProblemDetails?> RemoteWriteRequest(WriteRequest writeData, CancellationToken cancellationToken) {
    using var httpClient = new HttpClient();

    using var buffer = new MemoryStream();
    using var protobufWriter = new CodedOutputStream(buffer);
    writeData.WriteTo(protobufWriter);
    protobufWriter.Flush();

    // Compress
    var compressedData = SnappyCodec.Compress(buffer.ToArray());
    using var compressedBuffer = new MemoryStream(compressedData);

    var response = await httpClient.PostAsync(
      this._prometheusUrl,
      new StreamContent(compressedBuffer) {
        Headers = {
          { "Content-Type", "application/x-protobuf" },
          { "Content-Encoding", "snappy" }
        }
      },
      cancellationToken
    );

    if (!response.IsSuccessStatusCode) {
      var message = await response.Content.ReadAsStringAsync(cancellationToken);

      void HandleErrorMessageAndLog(ProblemDetails problemDetails, LogLevel level) {
        if (!String.IsNullOrWhiteSpace(message)) {
          message = message.Trim();
          problemDetails.Extensions.Add(key: "message", message);
          this._logger.Log(
            level,
            message: "Non-success response from Prometheus ({StatusCode}): {Message}",
            response.StatusCode,
            message
          );
        } else {
          this._logger.Log(
            level,
            message: "Non-success response from Prometheus ({StatusCode})",
            response.StatusCode
          );
        }
      }

      if ((response.StatusCode == HttpStatusCode.BadRequest) && !String.IsNullOrWhiteSpace(message)) {
        var problem = new ProblemDetails {
          Title = "Bad Request",
          Status = 400,
          Detail = "Invalid service health status."
        };
        HandleErrorMessageAndLog(problem, LogLevel.Debug);
        return problem;
      } else {
        var problem = new ProblemDetails {
          Title = "Internal Server Error",
          Status = 500,
          Detail = $"Unexpected response from Prometheus ({response.StatusCode})"
        };
        HandleErrorMessageAndLog(problem, LogLevel.Error);
        return problem;
      }
    }

    return null;
  }
}
