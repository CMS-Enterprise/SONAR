using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using Cms.BatCave.Sonar.Data;

namespace Cms.BatCave.Sonar.Authentication;

public class SonarIdentity : ClaimsIdentity {
  private readonly Guid _subjectId;
  private readonly SonarIdentityType _type;

  private readonly IImmutableList<ScopedPermission> _access;

  public override IEnumerable<Claim> Claims {
    get {
      yield return new Claim(SonarIdentityClaims.SubjectType, this._type.ToString());
      yield return new Claim(SonarIdentityClaims.SubjectId, this._subjectId.ToString());

      foreach (var access in this._access) {
        yield return new Claim(
          SonarIdentityClaims.Access,
          access.ToString()
        );
      }
    }
  }

  public SonarIdentity(ApiKey apiKey) :
    base(authenticationType: "apikey", nameType: null, roleType: null) {

    this._type = SonarIdentityType.ApiKey;
    this._subjectId = apiKey.Id;
    this._access =
      ImmutableList.Create(
        new ScopedPermission(apiKey.Type, apiKey.EnvironmentId, apiKey.TenantId)
      );
  }

  public SonarIdentity(User user, IEnumerable<UserPermission> permissions) :
    base(authenticationType: "sso", nameType: null, roleType: null) {

    this._type = SonarIdentityType.SsoUser;
    this._subjectId = user.Id;
    this._access =
      permissions.Select(p => new ScopedPermission(p.Permission, p.EnvironmentId, p.TenantId))
        .ToImmutableList();
  }
}
