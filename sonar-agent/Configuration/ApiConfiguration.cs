using System;

namespace Cms.BatCave.Sonar.Agent.Configuration;

public record ApiConfiguration(
  String Environment,
  String BaseUrl);
