using System;

namespace Cms.BatCave.Sonar.Configuration;

public record OktaConfiguration(
  String AuthorizationServerId,
  String Audience,
  String OktaDomain
);
