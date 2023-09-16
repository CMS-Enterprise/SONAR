using System;

namespace Cms.BatCave.Sonar.Agent.VersionChecks.Models;

public record VersionResponse(DateTime RequestTimestamp, String Version);
