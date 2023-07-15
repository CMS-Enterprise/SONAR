using System;

namespace Cms.BatCave.Sonar.Configuration;

public record OktaConfiguration(
  String OktaDomain,
  String? AuthorizationServerId = null,
  String? Audience = null
);
