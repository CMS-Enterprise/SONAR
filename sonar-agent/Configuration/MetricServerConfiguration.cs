using System;

namespace Cms.BatCave.Sonar.Agent.Configuration;

public record MetricServerConfiguration(
  Int32 Port = 2020);
