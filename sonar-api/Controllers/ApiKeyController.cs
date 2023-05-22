using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/keys")]
public class ApiKeyController : ControllerBase {
  private const Int32 ApiKeyByteLength = 32;

  private readonly DataContext _dbContext;
  private readonly DbSet<ApiKey> _apiKeysTable;
  private readonly EnvironmentDataHelper _envDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;


  public ApiKeyController(
    DataContext dbContext,
    DbSet<ApiKey> apiKeysTable,
    EnvironmentDataHelper envDataHelper,
    TenantDataHelper tenantDataHelper,
    ApiKeyDataHelper apiKeyDataHelper, DbSet<Environment> environmentsTable, DbSet<Tenant> tenantsTable) {

    this._dbContext = dbContext;
    this._apiKeysTable = apiKeysTable;
    this._envDataHelper = envDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._apiKeyDataHelper = apiKeyDataHelper;
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

    const String adminActivity = "create an API key";

    // Validate
    var isAdmin = await this._apiKeyDataHelper.ValidateAdminPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(),
      global: true, adminActivity, cancellationToken);
    if (!isAdmin) {
      throw new ForbiddenException(
        $"The authentication credential provided is not authorized to {adminActivity}.");
    }

    ValidateEnvAndTenant(apiKeyDetails.Environment, apiKeyDetails.Tenant);

    DBRepository dbRepository = new DBRepository(this._dbContext, this._apiKeysTable, this._envDataHelper,
      this._tenantDataHelper, this._environmentsTable, this._tenantsTable, cancellationToken);

    var task = dbRepository.Add(apiKeyDetails);
    var createdApiKey = await task;
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
  [Consumes(contentType: "application/json")]
  [ProducesResponseType(typeof(ApiKeyConfiguration), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> GetApiKeys(
    CancellationToken cancellationToken = default) {

    //ActionResult response;
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

    DBRepository dbRepository = new DBRepository(this._dbContext, this._apiKeysTable, this._envDataHelper,
      this._tenantDataHelper, this._environmentsTable, this._tenantsTable, cancellationToken);

    var task = dbRepository.GetKeys();
    var result = await task;
    return this.StatusCode((Int32)HttpStatusCode.Created, result);
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
    [FromRoute] String keyid,
    CancellationToken cancellationToken = default) {

    const String adminActivity = "delete an API key";

    // Validate
    var isAdmin = await this._apiKeyDataHelper.ValidateAdminPermission(
      this.Request.Headers["ApiKey"].SingleOrDefault(), global: true, adminActivity, cancellationToken);
    if (!isAdmin) {
      throw new ForbiddenException($"The authentication credential provided is not authorized to {adminActivity}.");
    }

    DBRepository dbRepository = new DBRepository(this._dbContext, this._apiKeysTable, this._envDataHelper,
      this._tenantDataHelper, this._environmentsTable, this._tenantsTable, cancellationToken);

    var task = dbRepository.Delete(new Guid(keyid));
    var result = await task;
    return this.StatusCode((Int32)HttpStatusCode.NoContent, result);
  }

  private static void ValidateEnvAndTenant(
    [NotNull] String? environment, String? tenant) {
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
