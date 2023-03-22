using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Mvc;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/environments")]
public class EnvironmentController : ControllerBase {
  private readonly EnvironmentDataHelper _envDataHelper;

  public EnvironmentController(
    EnvironmentDataHelper envDataHelper) {
    this._envDataHelper = envDataHelper;
  }

  [HttpGet]
  [ProducesResponseType(typeof(Environment), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetEnvironments(
    CancellationToken cancellationToken = default) {

    var environmentList = await this._envDataHelper.FetchAllExistingEnvAsync(
      cancellationToken);
    return this.Ok(environmentList);
  }
}
