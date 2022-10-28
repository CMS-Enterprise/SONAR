using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Snappy;
using Prometheus;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/health")]
public class HealthController : ControllerBase {
  private readonly IOptions<PrometheusConfiguration> _prometheusConfig;

  public HealthController(IOptions<PrometheusConfiguration> prometheusConfig) {
    this._prometheusConfig = prometheusConfig;
  }

  [HttpPost("{environment}/tenants/{tenant}/services/{service}/test")]
  public async Task<IActionResult> Test(
    [FromRoute]
    String environment,
    [FromRoute]
    String tenant,
    [FromRoute]
    String service,
    [FromBody]
    Double value,
    CancellationToken cancellationToken = default) {

    var writeData =
      new WriteRequest {
        Metadata = {
          new MetricMetadata {
            Help = "Test metric",
            Type = MetricMetadata.Types.MetricType.Gauge,
            MetricFamilyName = "sonar_test_metric"
          }
        },
        Timeseries = {
          new TimeSeries {
            Labels = {
              new Label { Name = "__name__", Value = "sonar_test_metric" },
              new Label { Name = "environment", Value = environment },
              new Label { Name = "tenant", Value = tenant },
              new Label { Name = "service", Value = service }
            },
            Samples = {
              new Sample {
                Timestamp = (Int64)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds,
                Value = value
              }
            }
          }
        }
      };

    using var httpClient = new HttpClient();

    using var buffer = new MemoryStream();
    using var protobufWriter = new CodedOutputStream(buffer);
    writeData.WriteTo(protobufWriter);
    protobufWriter.Flush();

    // reset the buffer
    buffer.Seek(offset: 0, SeekOrigin.Begin);

    // Compress
    var compressedData = SnappyCodec.Compress(buffer.ToArray());
    using var compressedBuffer = new MemoryStream(compressedData);

    var response = await httpClient.PostAsync(
      new Uri($"{this._prometheusConfig.Value.Protocol}://{this._prometheusConfig.Value.Host}:{this._prometheusConfig.Value.Port}/api/v1/write"),
      new StreamContent(compressedBuffer) {
        Headers = {
          { "Content-Type", "application/x-protobuf" },
          { "Content-Encoding", "snappy" }
        }
      },
      cancellationToken
    );

    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    if (!String.IsNullOrWhiteSpace(body)) {
      return this.StatusCode(
        (Int32)response.StatusCode,
        body
      );
    } else {
      return this.StatusCode((Int32)response.StatusCode);
    }
  }
}
