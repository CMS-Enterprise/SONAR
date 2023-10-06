using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Cms.BatCave.Sonar.Agent.Telemetry;

public class RuntimeCounterEventListener : EventListener {
  private readonly Meter _systemRuntimeMeter = new("System.Runtime");
  private Double _cpuUsage = 0;

  public RuntimeCounterEventListener() {
    this._systemRuntimeMeter.CreateObservableGauge(
      "process_runtime_dotnet_cpu_usage",
      () => this._cpuUsage,
      description: "The CPU usage of the current process reported by the dotnet runtime."
    );
  }

  protected override void OnEventSourceCreated(EventSource eventSource) {
    if (eventSource.Name == "System.Runtime") {
      this.EnableEvents(
        eventSource,
        EventLevel.LogAlways,
        EventKeywords.All,
        arguments: new Dictionary<String, String?> {
          ["EventCounterIntervalSec"] = "1"
        }
      );
    }

    base.OnEventSourceCreated(eventSource);
  }

  protected override void OnEventWritten(EventWrittenEventArgs eventData) {
    if (eventData.EventName == "EventCounters") {
      if (eventData.Payload?.FirstOrDefault() is IDictionary<String, Object> payload) {
        if (payload["Name"] is "cpu-usage") {
          this._cpuUsage = (Double)payload["Mean"];
        }
      }
    }

    base.OnEventWritten(eventData);
  }
}
