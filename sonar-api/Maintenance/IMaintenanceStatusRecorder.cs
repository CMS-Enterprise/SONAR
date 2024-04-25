using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cms.BatCave.Sonar.Maintenance;

public interface IMaintenanceStatusRecorder {
  Task RecordAsync(DateTime when, TimeSpan recordingPeriod, CancellationToken ct);
}
