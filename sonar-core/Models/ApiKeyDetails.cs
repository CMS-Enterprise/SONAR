using System;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ApiKeyDetails(
  ApiKeyType ApiKeyType,
  String? Environment,
  String? Tenant
);
