using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Authentication;
using Cms.BatCave.Sonar.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Middlewares;

public class UserPermissionClaimsMiddleware : Middleware<DbSet<User>, DbSet<UserPermission>> {
  public UserPermissionClaimsMiddleware(
    RequestDelegate next) : base(next) {
  }

  public override async Task InvokeAsync(
    HttpContext context,
    DbSet<User> userTable,
    DbSet<UserPermission> permissionTable) {

    var email = context.User.FindFirstValue(ClaimTypes.Email);

    if (!String.IsNullOrEmpty(email)) {
      var user = await userTable.SingleOrDefaultAsync(u => u.Email == email, context.RequestAborted);

      if (user != null) {
        var permissions = await permissionTable.Where(p => p.UserId == user.Id).ToListAsync();
        context.User.AddIdentity(new SonarIdentity(user, permissions));
      }
    }

    await this.Next(context);
  }
}
