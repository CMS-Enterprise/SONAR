using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record StatusCodeCondition(
  UInt16[] StatusCodes,
  HealthStatus Status
) : HttpHealthCheckCondition(Status, HttpHealthCheckConditionType.HttpStatusCode);
