using System;

namespace Cms.BatCave.Sonar.Data;

// ReSharper disable once InconsistentNaming
public interface Maintenance {
  Guid Id { get; init; }
  Boolean IsRecording { get; init; }
  DateTime? LastRecorded { get; init; }
  String MaintenanceScope { get; }
  String MaintenanceType { get; }
}
