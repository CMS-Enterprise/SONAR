using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
  private readonly IHttpContextAccessor _context;
  public const String TestAuthScheme = "FakeJwt";

  public TestAuthHandler(
    IHttpContextAccessor context,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : base(options, logger, encoder) {
    this._context = context;
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
    if (!String.IsNullOrEmpty(this._context.HttpContext?.Request.Headers.Authorization.FirstOrDefault())) {
      var auth = this._context.HttpContext.Request.Headers.Authorization.First()!;
      var authParts = auth.Split(' ');
      if (authParts[0] == TestAuthScheme) {
        var email = Encoding.UTF8.GetString(Convert.FromBase64String(authParts[1]));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
          new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Email, email)
          })),
          TestAuthScheme
        )));
      }
    }

    return Task.FromResult(AuthenticateResult.NoResult());
  }
}
