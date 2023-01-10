using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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
[Route("api/key")]
public class ApiKeyController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<ApiKey> _apiKeysTable;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;

  public ApiKeyController(
    DataContext dbContext,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<ApiKey> apiKeysTable,
    ApiKeyDataHelper apiKeyDataHelper) {

    this._dbContext = dbContext;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._apiKeysTable = apiKeysTable;
    this._apiKeyDataHelper = apiKeyDataHelper;
  }

  /// <summary>
  ///   Generates and encodes API key value.
  /// </summary>
  /// <returns>The encoded API key value.</returns>
  public static String GenerateApiKeyValue() {
    Int32 apiKeyByteLength = 32;
    var apiKey = new Byte[apiKeyByteLength];
    String encodedApiKey = "";

    using (var rng = RandomNumberGenerator.Create()) {
      // Generate API key
      rng.GetBytes(apiKey);

      // Encode API key
      encodedApiKey = Convert.ToBase64String(apiKey);
    }

    return encodedApiKey;
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
  [ProducesResponseType(typeof(ApiKey), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> CreateApiKey(
    [FromBody] ApiKeyDetails apiKeyDetails,
    CancellationToken cancellationToken = default) {

    ActionResult response;

    // Validate
    await this._apiKeyDataHelper.ValidateAdminPermission(
      Request.Headers["ApiKey"].Single(),
      "create an API key",
      cancellationToken);

    // Check if both environment and tenant are detailed
    if ((apiKeyDetails.Environment == null) && (apiKeyDetails.Tenant != null)) {
      throw new BadRequestException(
        message: "Tenant is in configuration, but Environment is missing.",
        ProblemTypes.InvalidConfiguration
      );
    } else if ((apiKeyDetails.Environment != null) && (apiKeyDetails.Tenant == null)) {
      throw new BadRequestException(
        message: "Environment is in configuration, but Tenant is missing.",
        ProblemTypes.InvalidConfiguration
      );
    }

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

    try {
      // Obtain Tenant ID if tenant is in configuration
      Guid? tenantId = null;
      if (apiKeyDetails.Tenant != null) {
        tenantId = await this._apiKeyDataHelper.FetchExistingTenantId(
          apiKeyDetails.Environment,
          apiKeyDetails.Tenant,
          cancellationToken);
      }

      // Record new API key
      var createdApiKey = await this._apiKeysTable.AddAsync(
        new ApiKey(GenerateApiKeyValue(), apiKeyDetails.ApiKeyType, tenantId),
        cancellationToken
      );
      await this._dbContext.SaveChangesAsync(cancellationToken);

      response = this.StatusCode((Int32)HttpStatusCode.Created, createdApiKey.Entity);
      await tx.CommitAsync(cancellationToken);
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    return response;
  }

  /// <summary>
  ///   Updates configuration for existing API key.
  /// </summary>
  /// <param name="apiKeyConfig">The API key ,type and, if they exist, environment and tenant.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The API key configuration led to a successful update.</response>
  /// <response code="400">The API key configuration is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for updating an API key.</response>
  /// <response code="404">The specified API key, environment, or tenant was not found.</response>
  [HttpPut]
  [Consumes(typeof(ApiKeyConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ApiKey), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> UpdateApiKey(
    [FromBody] ApiKeyConfiguration apiKeyConfig,
    CancellationToken cancellationToken = default) {

    ActionResult response;

    // Validate
    await this._apiKeyDataHelper.ValidateAdminPermission(
      Request.Headers["ApiKey"].Single(),
      "update an API key",
      cancellationToken);
    ApiKeyController.ValidateApiKeyConfig(apiKeyConfig);

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

    try {
      // Get API key
      var existingApiKey = await this._apiKeysTable
        .Where(k => k.Key == apiKeyConfig.ApiKey)
        .SingleOrDefaultAsync(cancellationToken);
      if (existingApiKey == null) {
        throw new ResourceNotFoundException(nameof(ApiKey), apiKeyConfig.ApiKey);
      }

      // Check API key type
      if (existingApiKey.Type != apiKeyConfig.ApiKeyType) {
        existingApiKey.Type = apiKeyConfig.ApiKeyType;
      }

      // Check tenant
      Guid? tenantId = null;
      if (apiKeyConfig.Tenant != null) {
        tenantId = await this._apiKeyDataHelper.FetchExistingTenantId(
          apiKeyConfig.Environment,
          apiKeyConfig.Tenant,
          cancellationToken);

        if (existingApiKey.TenantId != tenantId) {
          existingApiKey.TenantId = tenantId;
        }
      }

      // Update
      this._apiKeysTable.Update(existingApiKey);

      // Save
      await this._dbContext.SaveChangesAsync(cancellationToken);
      response = this.Ok(existingApiKey);
      await tx.CommitAsync(cancellationToken);
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    return response;
  }

  /// <summary>
  ///   Deletes existing API key.
  /// </summary>
  /// <param name="apiKeyConfig">The API key ,type and, if they exist, environment and tenant.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The API key configuration led to a successful deletion.</response>
  /// <response code="400">The API key configuration is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for deleting an API key.</response>
  /// <response code="404">The specified API key, environment, or tenant was not found.</response>
  [HttpDelete]
  [Consumes(typeof(ApiKeyConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ApiKey), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> DeleteApiKey(
    [FromBody] ApiKeyConfiguration apiKeyConfig,
    CancellationToken cancellationToken = default) {

    // Validate
    await this._apiKeyDataHelper.ValidateAdminPermission(
      Request.Headers["ApiKey"].Single(),
      "delete an API key",
      cancellationToken);
    ApiKeyController.ValidateApiKeyConfig(apiKeyConfig);

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

    try {
      // Check that the request contains all details of API key to delete

      // Get API key
      var existingApiKey = await this._apiKeysTable
        .Where(k => k.Key == apiKeyConfig.ApiKey)
        .SingleOrDefaultAsync(cancellationToken);
      if (existingApiKey == null) {
        throw new ResourceNotFoundException(nameof(ApiKey), apiKeyConfig.ApiKey);
      }

      // Check API key type
      if (existingApiKey.Type != apiKeyConfig.ApiKeyType) {
        throw new BadRequestException(
          message: "Provided API key type does not match.",
          ProblemTypes.InvalidConfiguration
        );
      }

      // Check tenant
      Guid? tenantId = null;
      if (apiKeyConfig.Tenant != null) {
        tenantId = await this._apiKeyDataHelper.FetchExistingTenantId(
          apiKeyConfig.Environment,
          apiKeyConfig.Tenant,
          cancellationToken);

        if (existingApiKey.TenantId != tenantId) {
          throw new BadRequestException(
            message: "Provided API key tenant does not match.",
            ProblemTypes.InvalidConfiguration
          );
        }
      }

      // Delete
      this._apiKeysTable.Remove(existingApiKey);

      // Save
      await this._dbContext.SaveChangesAsync(cancellationToken);
      await tx.CommitAsync(cancellationToken);
    } catch {
      await tx.RollbackAsync(cancellationToken);
      throw;
    }

    return this.StatusCode((Int32)HttpStatusCode.OK);
  }

  private static void ValidateApiKeyConfig(ApiKeyConfiguration apiKeyConfig) {

    // Check if configured API key is Base64 and of correct length
    Int32 apiKeyByteLength = 32;
    try {
      var decodedBytes = Convert.FromBase64String(apiKeyConfig.ApiKey);

      if (decodedBytes.Length != apiKeyByteLength) {
        throw new BadRequestException(
          message: "Configured API key to update is of an invalid length.",
          ProblemTypes.InvalidConfiguration
        );
      }
    } catch {
      throw new BadRequestException(
        message: "Configured API key to update is not Base64 encoded.",
        ProblemTypes.InvalidConfiguration
      );
    }

    // Check if both environment and tenant are configured
    if ((apiKeyConfig.Environment == null) && (apiKeyConfig.Tenant != null)) {
      throw new BadRequestException(
        message: "Tenant is in configuration, but Environment is missing.",
        ProblemTypes.InvalidConfiguration
      );
    } else if ((apiKeyConfig.Environment != null) && (apiKeyConfig.Tenant == null)) {
      throw new BadRequestException(
        message: "Environment is in configuration, but Tenant is missing.",
        ProblemTypes.InvalidConfiguration
      );
    }
  }
}
