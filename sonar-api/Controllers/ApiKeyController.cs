using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/keys")]
public class ApiKeyController : ControllerBase {
  private const String ApiKeyHeader = "ApiKey";

  private readonly IConfiguration _configuration;
  private readonly IApiKeyRepository _apiKeys;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;

  public ApiKeyController(
    IConfiguration configuration,
    IApiKeyRepository apiKeys,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable) {

    this._configuration = configuration;
    this._apiKeys = apiKeys;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
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

    const String activity = "create an API key";

    var encKey = this.Request.Headers[ApiKeyHeader].SingleOrDefault();
    if (encKey == null) {
      throw new UnauthorizedException($"Authentication is required to {activity}.");
    }

    //Make sure the parameters to create a key are correct.
    this.ValidateParametersToCreateKey(apiKeyDetails);

    //if encKey matches key from configuration - admin privileges - create key.
    if (this.MatchDefaultApiKey(encKey) != null) {
      var createdApiKey = await this._apiKeys.AddAsync(apiKeyDetails, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey);
    } else {
      var environment =
        !String.IsNullOrEmpty(apiKeyDetails.Environment) ?
          await this._environmentsTable.FirstOrDefaultAsync(e => e.Name == apiKeyDetails.Environment,
            cancellationToken) :
          null;
      var tenant =
        !String.IsNullOrEmpty(apiKeyDetails.Tenant) ?
          await this._tenantsTable.FirstOrDefaultAsync(t => t.Name == apiKeyDetails.Tenant, cancellationToken) :
          null;
      //If permissions are good create the key
      await this.ValidatePermission(encKey, environment?.Id, tenant?.Id, activity, cancellationToken);
      var createdApiKey = await this._apiKeys.AddAsync(apiKeyDetails, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey);
    }
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

    var encKey = this.Request.Headers[ApiKeyHeader].SingleOrDefault();
    if (encKey == null) {
      throw new UnauthorizedException($"Authentication is required to {activity}.");
    }

    if (this.MatchDefaultApiKey(encKey) != null) {
      await this._apiKeys.DeleteAsync(keyId, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.NoContent);
    }

    var targetApiKey = await this._apiKeys.GetApiKeyFromApiKeyIdAsync(keyId, cancellationToken);

    await this.ValidatePermission(
      encKey,
      targetApiKey.EnvironmentId,
      targetApiKey.TenantId,
      activity,
      cancellationToken);

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
  [ProducesResponseType(typeof(IEnumerable<ApiKeyDetails>), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> GetApiKeys(
    CancellationToken cancellationToken = default) {

    const String activity = "Get list of API keys";
    var encKey = this.Request.Headers[ApiKeyHeader].SingleOrDefault();
    if (encKey == null) {
      throw new UnauthorizedException($"Authentication is required to {activity}.");
    }

    if (this.MatchDefaultApiKey(encKey) != null) {
      var result = await this._apiKeys.GetKeysAsync(cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.OK, result);
    }

    var permKey = await this._apiKeys.GetApiKeyFromEncKeyAsync(encKey, cancellationToken);

    List<ApiKeyConfiguration> results;
    switch (permKey.Type) {
      case ApiKeyType.Admin when !permKey.EnvironmentId.HasValue:
        results = await this._apiKeys.GetKeysAsync(cancellationToken);
        break;
      case ApiKeyType.Admin when permKey.EnvironmentId.HasValue && !permKey.TenantId.HasValue:
        results = await this._apiKeys.GetEnvKeysAsync(permKey.EnvironmentId.Value, cancellationToken);
        break;
      case ApiKeyType.Admin when permKey.EnvironmentId.HasValue && permKey.TenantId.HasValue:
        results = await this._apiKeys.GetTenantKeysAsync(permKey, cancellationToken);
        break;
      default:
        throw new ForbiddenException($"Api Key is not authorized to {activity}.");
    }

    return this.StatusCode((Int32)HttpStatusCode.OK, results);
  }

  private void ValidateParametersToCreateKey(ApiKeyDetails apiKeyDetails) {
    if ((apiKeyDetails.Tenant != null) && (apiKeyDetails.Environment == null)) {
      throw new BadRequestException(
        message: "Tenant is in configuration, but associated Environment is missing.",
        ProblemTypes.InvalidConfiguration
      );
    }
  }

  private async Task ValidatePermission(
    String encKey,
    Guid? environmentScope,
    Guid? tenantScope,
    String activity,
    CancellationToken cancellationToken) {

    //Get the client Api Key - Make sure the keys permissions allow it to perform an action.
    var authenticationKey = await this._apiKeys.GetApiKeyFromEncKeyAsync(encKey, cancellationToken);

    if (authenticationKey.Type != ApiKeyType.Admin) {
      throw new ForbiddenException($"The authentication credential provided is not authorized to {activity}.");
    }

    // Make sure that the Api Key being created is within the scope that the API client is restricted to
    if (authenticationKey.EnvironmentId.HasValue) {
      if (authenticationKey.TenantId.HasValue) {
        if (authenticationKey.TenantId != tenantScope) {
          throw new ForbiddenException(
            $"Api Key not authorized to {activity} on the specified Api Key because it has a different Tenant scope."
          );
        }
      } else if (authenticationKey.EnvironmentId != environmentScope) {
        throw new ForbiddenException(
          $"Api Key not authorized to {activity} on the specified Api Key because it has a different Tenant scope."
        );
      }
    }
  }

  private ApiKey? MatchDefaultApiKey(String? headerApiKey) {
    if (headerApiKey == null) {
      return null;
    }

    var defaultApiKey = this._configuration.GetValue<String>("ApiKey");
    if (!String.IsNullOrEmpty(defaultApiKey) && String.Equals(defaultApiKey, headerApiKey, StringComparison.Ordinal)) {
      return new ApiKey(
        Guid.Empty,
        defaultApiKey,
        ApiKeyType.Admin,
        environmentId: null,
        tenantId: null
      );
    } else {
      return null;
    }
  }
}
