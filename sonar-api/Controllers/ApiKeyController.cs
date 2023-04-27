using System;
using System.Data;
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

  public ApiKeyController(
    DataContext dbContext,
    DbSet<ApiKey> apiKeysTable,
    EnvironmentDataHelper envDataHelper,
    TenantDataHelper tenantDataHelper,
    ApiKeyDataHelper apiKeyDataHelper) {

    this._dbContext = dbContext;
    this._apiKeysTable = apiKeysTable;
    this._envDataHelper = envDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._apiKeyDataHelper = apiKeyDataHelper;
  }

  /// <summary>
  ///   Generates and encodes API key value.
  /// </summary>
  /// <returns>The encoded API key value.</returns>
  private static String GenerateApiKeyValue() {
    var apiKey = new Byte[ApiKeyByteLength];
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
  [ProducesResponseType(typeof(ApiKeyConfiguration), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> CreateApiKey(
    [FromBody] ApiKeyDetails apiKeyDetails,
    CancellationToken cancellationToken = default) {

    ActionResult response;
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

    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

    try {
      var environment =
        await this._envDataHelper.FetchExistingEnvAsync(
          apiKeyDetails.Environment,
          cancellationToken);

      Tenant? tenant = null;

      if (apiKeyDetails.Tenant != null) {
        tenant = await this._tenantDataHelper.FetchExistingTenantAsync(
          // Verified by ValidateEnvAndTenant
          apiKeyDetails.Environment!,
          apiKeyDetails.Tenant,
          cancellationToken);
      }

      // Record new API key
      var createdApiKey = await this._apiKeysTable.AddAsync(
        new ApiKey(
          GenerateApiKeyValue(),
          apiKeyDetails.ApiKeyType,
          environment?.Id,
          tenant?.Id),
        cancellationToken
      );
      await this._dbContext.SaveChangesAsync(cancellationToken);

      response = this.StatusCode(
        (Int32)HttpStatusCode.Created,
        ToApiKeyConfig(environment, tenant, createdApiKey.Entity));
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
  [ProducesResponseType(typeof(ApiKeyDetails), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> UpdateApiKey(
    [FromBody] ApiKeyConfiguration apiKeyConfig,
    CancellationToken cancellationToken = default) {

    ActionResult response;
    const String adminActivity = "update an API key";

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

      var env = await this._envDataHelper.FetchExistingEnvAsync(
        apiKeyConfig.Environment!,
        cancellationToken);

      Tenant? tenant = null;
      if (apiKeyConfig.Tenant != null) {
        tenant = await this._tenantDataHelper.FetchExistingTenantAsync(
          apiKeyConfig.Environment!,
          apiKeyConfig.Tenant,
          cancellationToken);

        if (existingApiKey.TenantId != tenant.Id) {
          // API key is now associated with a different tenant
          existingApiKey.TenantId = tenant.Id;
        }
      } else {
        if (existingApiKey.EnvironmentId != env.Id) {
          // API key is now associated with specified environment instead of tenant
          existingApiKey.EnvironmentId = env.Id;
          existingApiKey.TenantId = null;
        }
      }

      // Update
      this._apiKeysTable.Update(existingApiKey);

      // Save
      await this._dbContext.SaveChangesAsync(cancellationToken);
      response = this.Ok(new ApiKeyDetails(
        existingApiKey.Type,
        env.Name,
        tenant?.Name
      ));
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
  /// <response code="204">The API key configuration led to a successful deletion.</response>
  /// <response code="400">The API key configuration is not valid.</response>
  /// <response code="401">The API key in the header is not authorized for deleting an API key.</response>
  /// <response code="404">The specified API key, environment, or tenant was not found.</response>
  [HttpDelete]
  [Consumes(typeof(ApiKeyConfiguration), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> DeleteApiKey(
    [FromBody] ApiKeyConfiguration apiKeyConfig,
    CancellationToken cancellationToken = default) {

    const String adminActivity = "delete an API key";

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

      if (apiKeyConfig.Tenant != null) {
        // Check tenant
        var tenant = await this._tenantDataHelper.FetchExistingTenantAsync(
          apiKeyConfig.Environment!,
          apiKeyConfig.Tenant,
          cancellationToken);

        if (existingApiKey.TenantId != tenant.Id) {
          throw new BadRequestException(
            message: "Provided API key tenant does not match.",
            ProblemTypes.InvalidConfiguration
          );
        }
      } else {
        // Check environment
        var env = await this._envDataHelper.FetchExistingEnvAsync(
          apiKeyConfig.Environment,
          cancellationToken);

        if (existingApiKey.EnvironmentId != env.Id) {
          throw new BadRequestException(
            message: "Provided API key environment does not match.",
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

    return this.StatusCode((Int32)HttpStatusCode.NoContent);
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

    ApiKeyController.ValidateEnvAndTenant(apiKeyConfig.Environment, apiKeyConfig.Tenant);
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

  private static ApiKeyConfiguration ToApiKeyConfig(
    Data.Environment? environment,
    Tenant? tenant,
    ApiKey entity) {

    return new ApiKeyConfiguration(
      entity.Key,
      entity.Type,
      environment?.Name,
      tenant?.Name
    );
  }
}
