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

    var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!String.IsNullOrEmpty(sub)) {
      var user = await userTable.SingleOrDefaultAsync(u => u.Email == sub, context.RequestAborted);

      if (user != null) {
        var permissions = await permissionTable.Where(p => p.UserId == user.Id).ToListAsync();
        context.User.AddIdentity(new SonarIdentity(user, permissions));
      }
    }

    await this.Next(context);
  }
}
