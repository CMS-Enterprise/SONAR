using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Prometheus;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace Cms.BatCave.Sonar.Controllers;

#if DEBUG

[ApiController]
[Route("api/test")]
[ApiVersionNeutral]
public class TestDataController : ControllerBase {
  private readonly IPrometheusRemoteProtocolClient _remoteProtocolClient;

  public TestDataController(IPrometheusRemoteProtocolClient remoteProtocolClient) {
    this._remoteProtocolClient = remoteProtocolClient;
  }

  [HttpPost("data", Name = "SaveData")]
  public async Task<IActionResult> SaveData(
    [FromBody] MetricData metricData,
    CancellationToken cancellationToken = default) {

    var writeData = new WriteRequest {
      Metadata = {
        new MetricMetadata {
          Help = metricData.HelpText,
          Type = (MetricMetadata.Types.MetricType)metricData.MetricType,
          MetricFamilyName = metricData.MetricName
        }
      },
      Timeseries = {
        CreateMetricTimeSeries(metricData)
      }
    };

    await this._remoteProtocolClient.WriteAsync(writeData, cancellationToken);

    return this.NoContent();
  }

  private static TimeSeries CreateMetricTimeSeries(MetricData metricData) {
    var ts = new TimeSeries();
    ts.Labels.AddRange(
      metricData.Labels.Select(kvp => new Label {
        Name = kvp.Key,
        Value = kvp.Value
      }));
    ts.Samples.AddRange(
      metricData.TimeSeries.Select(sample => new Sample {
        Timestamp = (Int64)sample.timestamp.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds,
        Value = sample.value
      }));
    return ts;
  }
}

#endif
