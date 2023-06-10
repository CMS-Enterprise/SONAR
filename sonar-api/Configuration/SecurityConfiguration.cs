using System;

namespace Cms.BatCave.Sonar.Configuration;

public record SecurityConfiguration(
  String? DefaultApiKey = null,
  Int32 ApiKeyWorkFactor = 12);
