using System;

namespace Cms.BatCave.Sonar.Configuration;

public record LokiConfiguration(
  String Host,
  UInt16 Port = 3100,
  String Protocol = "http");
