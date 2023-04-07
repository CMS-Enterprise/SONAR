using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace Cms.BatCave.Sonar.Controllers;

#if DEBUG

[ApiController]
[Route("api/test")]
public class TestDataController : ControllerBase {
  private readonly PrometheusRemoteWriteClient _remoteWriteClient;

  public TestDataController(PrometheusRemoteWriteClient remoteWriteClient) {
    this._remoteWriteClient = remoteWriteClient;
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

    var problem = await this._remoteWriteClient.RemoteWriteRequest(writeData, cancellationToken);
    if (problem == null) {
      return this.NoContent();
    }

    return this.StatusCode(problem.Status ?? 500, problem);
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
