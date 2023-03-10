using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;

using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/environment")]
public class EnvironmentController : ControllerBase{
  private readonly EnvironmentDataHelper _envDataHelper;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;

  public EnvironmentController(
    EnvironmentDataHelper envDataHelper,
    ApiKeyDataHelper apiKeyDataHelper) {
    this._envDataHelper = envDataHelper;
    this._apiKeyDataHelper = apiKeyDataHelper;
  }

  [HttpGet]
  [ProducesResponseType(typeof(ServiceHierarchyConfiguration), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetTenant(
    CancellationToken cancellationToken = default) {

    String headerApiKey = Request.Headers["ApiKey"].SingleOrDefault();
    var activity = "request a list of a environments";

    // Validation
    Boolean isAdmin = await this._apiKeyDataHelper.ValidateAdminPermission(
      headerApiKey, activity, cancellationToken);

    if (!isAdmin) {
      throw new ForbiddenException($"The authentication credential provided is not authorized to {activity}.");
    }

    var environmentList = await this._envDataHelper.FetchAllExistingEnvAsync(
      cancellationToken);
    return this.Ok(environmentList);
  }
}
