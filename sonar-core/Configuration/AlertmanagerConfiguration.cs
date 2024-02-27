using System;

namespace Cms.BatCave.Sonar.Configuration;

public record AlertmanagerConfiguration(
  String Host,
  UInt16 Port = 9093,
  String Protocol = "http",
  Int32 RequestTimeoutSeconds = 10);
