using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ResponseTimeCondition(
  TimeSpan ResponseTime,
  HealthStatus Status) : HttpHealthCheckCondition(Status, HttpHealthCheckConditionType.HttpResponseTime);
