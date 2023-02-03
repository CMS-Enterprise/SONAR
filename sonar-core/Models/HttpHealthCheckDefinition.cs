using System;

namespace Cms.BatCave.Sonar.Models;

public record HttpHealthCheckDefinition(
  Uri Url,
  HttpHealthCheckCondition[] Conditions,
  Boolean? FollowRedirects,
  String? AuthorizationHeader,
  Boolean? SkipCertificateValidation) : HealthCheckDefinition();
