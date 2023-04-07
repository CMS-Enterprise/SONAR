using System;

namespace Cms.BatCave.Sonar.Configuration;

public record LegacyEndpointConfiguration(
  Boolean Enabled = false,
  LegacyServiceMapping[]? ServiceMapping = null,
  String[]? RootServices = null) {
}
