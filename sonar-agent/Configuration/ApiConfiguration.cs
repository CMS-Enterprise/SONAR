using System;

namespace Cms.BatCave.Sonar.Agent.Configuration;

public record ApiConfiguration(
  String Environment,
  String BaseUrl,
  String ApiKey,
  Guid? ApiKeyId = null
  );
