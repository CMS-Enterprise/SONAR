using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/keys")]
public class ApiKeyController : ControllerBase {
  private const Int32 ApiKeyByteLength = 32;

  private readonly ApiKeyDataHelper _apiKeyDataHelper;
  private readonly IApiKeyRepository _apiKeys;

  public ApiKeyController(
    ApiKeyDataHelper apiKeyDataHelper,
    IApiKeyRepository apiKeys) {

    this._apiKeyDataHelper = apiKeyDataHelper;
    this._apiKeys = apiKeys;
  }

  /// <summary>
  ///   Creates and records configuration for new API key.
  /// </summary>
  /// <param name="apiKeyDetails">The API key type and, if they exist, environment and tenant.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The API key details led to a successful API key creation.</response>
  /// <response code="400">The API key details are not valid.</response>
  /// <response code="401">The API key in the header is not authorized for creating an API key.</response>
  [HttpPost]
  [Consumes(typeof(ApiKeyDetails), contentType: "application/json")]
  [ProducesResponseType(typeof(ApiKeyConfiguration), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> CreateApiKey(
    [FromBody] ApiKeyDetails apiKeyDetails,
    CancellationToken cancellationToken = default) {

    const String adminActivity = "create an API key";

    // Validate
    var isAdmin = await this._apiKeyDataHelper.ValidateAdminPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(),
      global: true,
      adminActivity,
      cancellationToken
    );
    if (!isAdmin) {
      throw new ForbiddenException(
        $"The authentication credential provided is not authorized to {adminActivity}.");
    }

    ValidateEnvAndTenant(apiKeyDetails.Environment, apiKeyDetails.Tenant);

    var createdApiKey = await this._apiKeys.AddAsync(apiKeyDetails, cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey);
  }

  /// <summary>
  ///   Get API keys.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The key resources have been fetched and transmitted in the message body.</response>
  /// <response code="400">The API key details are not valid.</response>
  /// <response code="401">The API key in the header is not authorized for creating an API key.</response>
  [HttpGet]
  [ProducesResponseType(typeof(IEnumerable<ApiKeyConfiguration>), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> GetApiKeys(
    CancellationToken cancellationToken = default) {

    const String adminActivity = "Get list of API keys";

    // Validate
    var isAdmin = await this._apiKeyDataHelper.ValidateAdminPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(),
      global: true,
      adminActivity,
      cancellationToken
    );
    if (!isAdmin) {
      throw new ForbiddenException(
        $"The authentication credential provided is not authorized to {adminActivity}.");
    }

    var result = await this._apiKeys.GetKeysAsync(cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.OK, result);
  }

  /// <summary>
  ///   Deletes existing API key.
  /// </summary>
  /// <param name="keyid">ID of the key to delete</param>
  /// <param name="cancellationToken"></param>
  /// <response code="204">The API key configuration led to a successful deletion.</response>
  /// <response code="400">The API key configuration is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for deleting an API key.</response>
  /// <response code="404">The specified API key, environment, or tenant was not found.</response>
  [HttpDelete("{keyid}", Name = "DeleteApiKey")]
  [Consumes(contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> DeleteApiKey(
    [FromRoute] Guid keyid,
    CancellationToken cancellationToken = default) {

    const String adminActivity = "delete an API key";

    // Validate
    var isAdmin = await this._apiKeyDataHelper.ValidateAdminPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(), global: true, adminActivity, cancellationToken);
    if (!isAdmin) {
      throw new ForbiddenException($"The authentication credential provided is not authorized to {adminActivity}.");
    }

    await this._apiKeys.DeleteAsync(keyid, cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.NoContent);
  }

  private static void ValidateEnvAndTenant(
    [NotNull]
    String? environment,
    String? tenant) {
    // Check if environment and tenant are detailed
    if (environment == null) {
      if (tenant != null) {
        throw new BadRequestException(
          message: "Tenant is in configuration, but associated Environment is missing.",
          ProblemTypes.InvalidConfiguration
        );
      } else {
        throw new BadRequestException(
          message: "Environment is missing.",
          ProblemTypes.InvalidConfiguration
        );
      }
    }
  }

}
