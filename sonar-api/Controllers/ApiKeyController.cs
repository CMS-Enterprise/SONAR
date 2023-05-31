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
using Microsoft.Extensions.Configuration;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/keys")]
public class ApiKeyController : ControllerBase {
  private const String ApiKeyHeader = "ApiKey";

  private readonly IApiKeyRepository _apiKeys;
  private readonly IConfiguration _configuration;

  public ApiKeyController(
    IConfiguration configuration,
    IApiKeyRepository apiKeys) {

    this._configuration = configuration;
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

    const String activity = "create an API key";

    var encKey = this.Request.Headers[ApiKeyHeader].SingleOrDefault();
    if (encKey == null) {
      throw new BadRequestException("missing ApiKey header");
    }

    //Make sure the parameters to create a key are correct.
    this.ValidateParametersToCreateKey(apiKeyDetails);
    var requestDetails = this._apiKeys.GetKeyDetails(apiKeyDetails.ApiKeyType, apiKeyDetails.Environment, apiKeyDetails.Tenant);

    //if encKey matches key from configuration - admin privileges - create key.
    if (this.MatchDefaultApiKey(encKey) != null) {
      var createdApiKey = await this._apiKeys.AddAsync(requestDetails, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey);
    }

    //If permissions are good create the key
    var hasPermission = await HasPermission(encKey, requestDetails, activity, cancellationToken);
    if (hasPermission) {
      var createdApiKey = await this._apiKeys.AddAsync(requestDetails, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey);
    }

    return this.StatusCode((Int32)HttpStatusCode.BadRequest);
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

    const String activity = "delete API key";

    var encKey = this.Request.Headers[ApiKeyHeader].SingleOrDefault();
    if (encKey == null) {
      throw new ForbiddenException($"No authentication credential provided not authorized to {activity}.");
    }

    if (this.MatchDefaultApiKey(encKey) != null) {
      await this._apiKeys.DeleteAsync(keyid, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.NoContent);
    }

    var requestDetails = await this._apiKeys.GetKeyDetailsFromKeyIdAsync(keyid, cancellationToken);

    var hasPermission = await HasPermission(encKey, requestDetails, activity, cancellationToken);
    if (hasPermission) {
      await this._apiKeys.DeleteAsync(keyid, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.NoContent);
    }
    throw new ForbiddenException($"Api Key is not authorized to {activity}.");
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

    const String activity = "Get list of API keys";
    var encKey = this.Request.Headers[ApiKeyHeader].SingleOrDefault();
    if (encKey == null) {
      throw new BadRequestException("No ApiKey in header");
    }

    if (this.MatchDefaultApiKey(encKey) != null) {
      var result = await this._apiKeys.GetKeysAsync(cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.OK, result);
    }

    var permKey = await this._apiKeys.GetApiKeyFromEncKeyAsync(encKey, cancellationToken);

    List<ApiKeyConfiguration>? results = null;
    switch (permKey.Type) {
      case ApiKeyType.Admin:
        results = await this._apiKeys.GetKeysAsync(cancellationToken);
        break;
      case ApiKeyType.EnvAdmin:
        results = await this._apiKeys.GetEnvKeysAsync(permKey, cancellationToken);
        break;
      case ApiKeyType.TenantAdmin:
        results = await this._apiKeys.GetTenantKeysAsync(permKey, cancellationToken);
        break;
      default:
        throw new ForbiddenException($"Api Key is not authorized to {activity}.");
    }
    return this.StatusCode((Int32)HttpStatusCode.OK, results);
  }

  private void ValidateParametersToCreateKey(ApiKeyDetails apiKeyDetails) {

    if (apiKeyDetails.ApiKeyType == ApiKeyType.Admin) {
      if ((apiKeyDetails.Environment != null) || (apiKeyDetails.Tenant != null)) {
        throw new BadRequestException("To create this type of key, there cannot be an Environment or Tenant in the request.");
      }
    }
    if (apiKeyDetails.ApiKeyType == ApiKeyType.EnvAdmin) {
      if ((apiKeyDetails.Environment == null) || (apiKeyDetails.Tenant != null)) {
        throw new BadRequestException("To create this type of key, there can only be an Environment in the request and no Tenant.");
      }
    }
    if (apiKeyDetails.ApiKeyType == ApiKeyType.TenantAdmin) {
      if ((apiKeyDetails.Environment == null) || (apiKeyDetails.Tenant == null)) {
        throw new BadRequestException("To create this type of key there must be an Environment and Tenant in the request.");
      }
    }
  }

  private async Task<Boolean> HasPermission(String encKey, ApiKeyDetails requestDetails, String activity, CancellationToken cancellationToken) {
    //Get the client Api Key - Make sure the keys permissions allow it to perform an action.
    var apiKey = await this._apiKeys.GetApiKeyFromEncKeyAsync(encKey, cancellationToken);

    //Check the request against the client Api key to see if permission is good.
    //Switch on what Api Key you are trying to create - this is the request to make a key.
    switch (requestDetails.ApiKeyType) {
      case ApiKeyType.Admin:
        if (apiKey.Type != ApiKeyType.Admin) {
          throw new ForbiddenException($"Api Key not authorized to {activity}.");
        }
        break;
      case ApiKeyType.EnvAdmin:
        //Check does the client key have permission to perform this action
        if ((apiKey.Type != ApiKeyType.Admin) && (apiKey.Type != ApiKeyType.EnvAdmin)) {
          throw new ForbiddenException($"Api Key not authorized to {activity}.");
        }
        //If the client key is EnvAdmin, make sure the environment Ids are the same.
        //An environment key can only work on its on environment.
        if (apiKey.Type == ApiKeyType.EnvAdmin) {
          if (apiKey.EnvironmentId != requestDetails.EnvironmentId) {
            throw new ForbiddenException($"Api Key not authorized to {activity}.");
          }
        }

        break;
      case ApiKeyType.TenantAdmin:
        if ((apiKey.Type != ApiKeyType.Admin) && (apiKey.Type != ApiKeyType.EnvAdmin) && (apiKey.Type != ApiKeyType.TenantAdmin)) {
          throw new ForbiddenException($"Api Key not authorized to {activity}.");
        }
        //If the client key is EnvAdmin, make sure the environment Ids are the same.
        //An environment key can only work on its on environment.
        if (apiKey.Type == ApiKeyType.EnvAdmin) {
          if (apiKey.EnvironmentId != requestDetails.EnvironmentId) {
            throw new ForbiddenException($"Api Key not authorized to {activity}.");
          }
        }
        if (apiKey.Type == ApiKeyType.TenantAdmin) {
          if ((apiKey.EnvironmentId != requestDetails.EnvironmentId) || (apiKey.TenantId != requestDetails.TenantId)) {
            throw new ForbiddenException($"Api Key not authorized to {activity}.");
          }
        }
        break;
    }
    return true;
  }

  private ApiKey? MatchDefaultApiKey(String? headerApiKey) {
    if (headerApiKey == null)
      return null;
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
