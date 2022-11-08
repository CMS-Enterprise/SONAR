using System;
namespace Cms.BatCave.Sonar.Configuration;

public record ApiConfiguration(
  String Environment,
  String Tenant,
  String BaseUrl);
