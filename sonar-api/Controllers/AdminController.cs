using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersionNeutral]
[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase {
  private readonly DataContext _dataContext;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;

  public AdminController(
    DataContext dataContext,
    ApiKeyDataHelper apiKeyDataHelper) {

    this._dataContext = dataContext;
    this._apiKeyDataHelper = apiKeyDataHelper;
  }

  [HttpPost("initialize")]
  public async Task<IActionResult> InitializeDatabase(
    [FromQuery] String? confirmation = null,
    [FromQuery] Boolean force = false,
    CancellationToken cancellationToken = default) {

    if (force) {
      var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
      if (date == confirmation) {
        var deleted = await this._dataContext.Database.EnsureDeletedAsync(cancellationToken);
        return this.Ok(new {
          DatabaseDeleted = deleted,
          DatabaseCreated = await this._dataContext.Database.EnsureCreatedAsync(cancellationToken)
        });
      } else {
        throw new BadRequestException(
          confirmation == null ?
            "The current date (in UTC) must be provided to force database recreation." :
            $"The data provided to force database recreation ({confirmation}) did not match the current date on the server ({date}).",
          ProblemTypes.MissingOrIncorrectConfirmationCode
        );
      }
    } else {
      return this.Ok(new {
        DatabaseCreated = await this._dataContext.Database.EnsureCreatedAsync(cancellationToken)
      });
    }
  }
}
