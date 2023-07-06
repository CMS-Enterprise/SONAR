using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Authentication;

public class MultiSchemeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
  private readonly IAuthenticationSchemeProvider _schemes;
  public const String SchemeName = "SonarAuth";

  public MultiSchemeAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    IAuthenticationSchemeProvider schemes) : base(options, logger, encoder, clock) {

    this._schemes = schemes;
  }

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
    var handlers = this.Context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
    foreach (var scheme in await this._schemes.GetAllSchemesAsync()) {
      if (scheme.Name != SchemeName) {
        var handler = await handlers.GetHandlerAsync(this.Context, scheme.Name);

        if (handler != null) {
          var result = await handler.AuthenticateAsync();
          if (!result.None) {
            return result;
          }
        } else {
          this.Logger.LogWarning(
            "Handler not found for authentication scheme {Scheme}",
            scheme.Name
          );
        }
      }
    }

    return AuthenticateResult.NoResult();
  }
}
