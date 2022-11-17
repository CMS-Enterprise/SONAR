using System;

namespace Cms.BatCave.Sonar.Configuration;

public record PrometheusConfiguration(
  String Host,
  UInt16 Port = 9090,
  String Protocol = "http");
