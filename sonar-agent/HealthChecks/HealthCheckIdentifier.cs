using System;

namespace Cms.BatCave.Sonar.Agent.HealthChecks;

public record HealthCheckIdentifier(
  String Environment,
  String Tenant,
  String Service,
  String Name) {

  public override String ToString() {
    return $"{this.Environment}/{this.Tenant}/{this.Service}/{this.Name}";
  }
}
