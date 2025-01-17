using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Authentication;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "AllowAnyScope")]
[Route("api/v{version:apiVersion}/keys")]
public class ApiKeyController : ControllerBase {
  private readonly IApiKeyRepository _apiKeys;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;

  public ApiKeyController(
    IApiKeyRepository apiKeys,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable) {

    this._apiKeys = apiKeys;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
  }

  /// <summary>
  ///   Creates and records configuration for new API key. The user performing this request must have sufficient
  ///   permissions to the requested environment/tenant.
  /// </summary>
  /// <param name="apiKeyDetails">The API key type and, if they exist, environment and tenant.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="201">The API key details led to a successful API key creation.</response>
  /// <response code="400">The API key details are not valid.</response>
  /// <response code="401">The API key in the header is not authorized for creating an API key.</response>
  [HttpPost]
  [Consumes(typeof(ApiKeyDetails), contentType: "application/json")]
  [ProducesResponseType(typeof(ApiKeyConfiguration), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [Authorize(Policy = "AllowAnyScope")]
  public async Task<ActionResult> CreateApiKey(
    [FromBody] ApiKeyDetails apiKeyDetails,
    CancellationToken cancellationToken = default) {

    const String activity = "create an API key";

    //Make sure the parameters to create a key are correct.
    ValidateParametersToCreateKey(apiKeyDetails);

    var environment =
      !String.IsNullOrEmpty(apiKeyDetails.Environment) ?
        await this._environmentsTable.FirstOrDefaultAsync(e => e.Name == apiKeyDetails.Environment,
          cancellationToken) :
        null;
    var tenant =
      !String.IsNullOrEmpty(apiKeyDetails.Tenant) ?
        await this._tenantsTable.FirstOrDefaultAsync(t => t.Name == apiKeyDetails.Tenant, cancellationToken) :
        null;

    //If permissions are good, create the key
    ValidationHelper.ValidatePermissionScope(
      this.User,
      environment?.Id,
      tenant?.Id,
      activity);

    var createdApiKey = await this._apiKeys.AddAsync(apiKeyDetails, cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey);
  }

  /// <summary>
  ///   Deletes existing API key.
  /// </summary>
  /// <param name="keyId">ID of the key to delete</param>
  /// <param name="cancellationToken"></param>
  /// <response code="204">The API key configuration led to a successful deletion.</response>
  /// <response code="400">The API key configuration is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for deleting an API key.</response>
  /// <response code="404">The specified API key, environment, or tenant was not found.</response>
  [HttpDelete("{keyId}", Name = "DeleteApiKey")]
  [Consumes(contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> DeleteApiKey(
    [FromRoute] Guid keyId,
    CancellationToken cancellationToken = default) {

    const String activity = "delete API key";

    var targetApiKey = await this._apiKeys.FindAsync(keyId, cancellationToken);

    ValidationHelper.ValidatePermissionScope(
      this.User,
      targetApiKey.EnvironmentId,
      targetApiKey.TenantId,
      activity);

    await this._apiKeys.DeleteAsync(keyId, cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.NoContent);
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

    List<ApiKeyConfiguration> results = new();
    var hasAdminAccess = false;

    foreach (var accessScope in this.User.GetEffectivePermissions()) {
      if (accessScope.HasPermission(PermissionType.Admin)) {
        hasAdminAccess = true;

        if (accessScope.EnvironmentId.HasValue) {
          if (accessScope.TenantId.HasValue) {
            results.AddRange(await this._apiKeys.GetTenantKeysAsync(
              accessScope.EnvironmentId.Value,
              accessScope.TenantId.Value,
              cancellationToken
            ));
          } else {
            results.AddRange(await this._apiKeys.GetEnvKeysAsync(accessScope.EnvironmentId.Value, cancellationToken));
          }
        } else {
          // Since the user has global access this set of keys should cover everything they have
          // access to (no need to AddRange)
          results = await this._apiKeys.GetKeysAsync(cancellationToken);
        }
      }
    }

    if (!hasAdminAccess) {
      throw new ForbiddenException($"Api Key is not authorized to list API Keys.");
    }

    return this.StatusCode((Int32)HttpStatusCode.OK, results);
  }

  private static void ValidateParametersToCreateKey(ApiKeyDetails apiKeyDetails) {
    if ((apiKeyDetails.Tenant != null) && (apiKeyDetails.Environment == null)) {
      throw new BadRequestException(
        message: "Tenant is in configuration, but associated Environment is missing.",
        ProblemTypes.InvalidConfiguration
      );
    }
  }
}
