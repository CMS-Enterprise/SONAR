using System;

namespace Cms.BatCave.Sonar.Agent.Configuration;
public class SonarTenantCreatedEventArgs : EventArgs {
  public String Tenant { get; }

  public SonarTenantCreatedEventArgs(String tenant) {
    this.Tenant = tenant;
  }
}
