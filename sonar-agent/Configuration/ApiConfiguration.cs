using System;
using System.Collections.Immutable;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.Configuration;

public record ApiConfiguration(
  String Environment,
  String BaseUrl,
  String ApiKey,
  Guid? ApiKeyId = null,
  Boolean? IsNonProd = null,
  ScheduledMaintenanceConfiguration[]? ScheduledMaintenances = null);
