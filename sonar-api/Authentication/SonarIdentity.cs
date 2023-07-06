using System;
using System.Collections.Generic;
using System.Security.Claims;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using System.Security.Cryptography;
using System.Text;

namespace Cms.BatCave.Sonar.Authentication;

public class SonarIdentity : ClaimsIdentity {
  public Guid ApiKeyId { get; }
  public PermissionType ApiKeyType { get; }

  public Guid? EnvironmentId { get; }
  public Guid? TenantId { get; }

  public override IEnumerable<Claim> Claims {
    get {
      yield return new Claim(SonarIdentityClaims.Subject, this.ApiKeyId.ToString());
      yield return new Claim(SonarIdentityClaims.Type, this.ApiKeyType.ToString());

      if (this.EnvironmentId.HasValue) {
        yield return new Claim(SonarIdentityClaims.Environment, this.EnvironmentId.Value.ToString());
      }

      if (this.TenantId.HasValue) {
        yield return new Claim(SonarIdentityClaims.Tenant, this.TenantId.Value.ToString());
      }
    }
  }

  public SonarIdentity(ApiKey apiKey) :
    base(authenticationType: "sonar", nameType: null, roleType: SonarIdentityClaims.Type) {

    this.ApiKeyId = apiKey.Id;
    this.ApiKeyType = apiKey.Type;
    this.EnvironmentId = apiKey.EnvironmentId;
    this.TenantId = apiKey.TenantId;
  }
}
