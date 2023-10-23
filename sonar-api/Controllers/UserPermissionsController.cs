using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Authentication;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/permissions")]
public class UserPermissionsController : ControllerBase {
  private readonly IPermissionsRepository _permissionsRepository;
  private readonly IApiKeyRepository _apiKeys;

  public UserPermissionsController(IPermissionsRepository permissionsRepository, IApiKeyRepository apiKeys) {
    this._permissionsRepository = permissionsRepository;
    this._apiKeys = apiKeys;
  }

  /// <summary>
  ///   Create a user permission. The user performing the request must have sufficient permissions
  ///   to the requested environment/tenant.
  /// </summary>
  /// <param name="permissionDetails">Add permission for specified user(email)</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The parameters led to a successful permission creation.</response>
  /// <response code="400">The parameters are not valid.</response>
  /// <response code="401">The user is not authorized to create a permission.</response>
  [Authorize(Policy = "AllowAnyScope")]
  [HttpPost]
  [Consumes(typeof(PermissionDetails), contentType: "application/json")]
  [ProducesResponseType(typeof(PermissionConfiguration), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  public async Task<ActionResult> CreatePermission(
    [FromBody] PermissionDetails permissionDetails,
    CancellationToken cancellationToken = default) {

    const String activity = "create permission";

    var (user, environment, tenant) = await this._permissionsRepository.GetObjectsFromNames(
      permissionDetails.UserEmail,
permissionDetails.Environment,
      permissionDetails.Tenant,
      cancellationToken);

    ValidatePermissionDetails(permissionDetails, user, environment, tenant);


    //If permissions are valid create user permissions
    ValidatePermission(this.User, environment?.Id, tenant?.Id, activity);
    var createdUserPermission = await this._permissionsRepository.AddAsync(
      new UserPermission(Guid.Empty, user!.Id, environment?.Id, tenant?.Id, permissionDetails.Permission),
      cancellationToken);
    return this.StatusCode(
      (Int32)HttpStatusCode.Created,
      ToPermissionConfig(environment, tenant, user.Email, createdUserPermission));
  }

  /// <summary>
  ///   Delete a user permission.
  /// </summary>
  /// <param name="permissionId">ID of permission to delete</param>
  /// <param name="cancellationToken"></param>
  /// <response code="204">The user permission successfully deleted.</response>
  /// <response code="400">The user permission is not valid.</response>
  /// <response code="401">User not authorized to delete specified user permission.</response>
  /// <response code="404">The specified user permission was not found.</response>
  [Authorize(Policy = "AllowAnyScope")]
  [HttpDelete("{permissionId}", Name = "DeleteUserPermission")]
  [Consumes(contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> DeleteUserPermission(
    [FromRoute] Guid permissionId,
    CancellationToken cancellationToken = default) {

    const String activity = "delete User Permission";

    var userPermission = await this._permissionsRepository.GetAsync(permissionId, cancellationToken);

    if (userPermission != null) {
      ValidatePermission(this.User, userPermission.EnvironmentId, userPermission.TenantId, activity);
    } else {
      throw new ResourceNotFoundException("UserPermission");
    }

    await this._permissionsRepository.DeleteAsync(permissionId, cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.NoContent);
  }

  /// <summary>
  ///   Update a user permission.
  /// </summary>
  /// <param name="permissionId">ID of user permission to update</param>
  /// <param name="permissionDetails">Add permission for specified user(email)</param>
  /// <param name="cancellationToken"></param>
  /// <response code="204">The user permission has been updated.</response>
  /// <response code="400">The user permission is not valid.</response>
  /// <response code="401">User not authorized to update user permission.</response>
  /// <response code="404">The specified user permission was not found.</response>
  [HttpPut("{permissionId}", Name = "UpdateUserPermission")]
  [Consumes(typeof(PermissionDetails), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> UpdateUserPermission(
    [FromRoute] Guid permissionId,
    [FromBody] PermissionDetails permissionDetails,
    CancellationToken cancellationToken = default) {

    const String activity = "update User Permission";

    var (user, environment, tenant) = await this._permissionsRepository.GetObjectsFromNames(
      permissionDetails.UserEmail,
      permissionDetails.Environment,
      permissionDetails.Tenant,
      cancellationToken);

    ValidatePermissionDetails(permissionDetails, user, environment, tenant);

    var userPermission = await this._permissionsRepository.GetAsync(permissionId, cancellationToken);

    if (userPermission != null) {
      ValidatePermission(this.User, userPermission.EnvironmentId, userPermission.TenantId, activity);
    } else {
      throw new ResourceNotFoundException("UserPermission");
    }

    await this._permissionsRepository.UpdateAsync(
      new UserPermission(permissionId, user!.Id, environment?.Id, tenant?.Id, permissionDetails.Permission),
      cancellationToken);
    return this.StatusCode((Int32)HttpStatusCode.NoContent);
  }

  /// <summary>
  /// Get permissions of the current user.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <response code="200">Successfully retrieved current user permissions.</response>
  /// <response code="401">The user is not authorized to retrieve its permissions.</response>
  [Authorize(Policy = "AllowAnyScope")]
  [HttpGet("me", Name = "GetCurrentUser")]
  [ProducesResponseType(typeof(PermissionConfiguration[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [ProducesResponseType(500)]
  public async Task<ActionResult> GetCurrentUser(
    CancellationToken cancellationToken = default) {

    //if SonarSubjectType == "ApiKey"  then SonarSubjectId == ApiKeyId
    //if SonarSubjectType == "Sso"  then SonarSubjectId == UserId
    var access = User.FindFirst(x => x.Type.Equals("SonarAccess", StringComparison.OrdinalIgnoreCase))?.Value;
    var subjectId = User.FindFirst(x => x.Type.Equals("SonarSubjectId", StringComparison.OrdinalIgnoreCase))?.Value;
    var subjectType = User.FindFirst(x => x.Type.Equals("SonarSubjectType", StringComparison.OrdinalIgnoreCase))?.Value;

    // return 401 (Unauthorized) if claims are missing
    if (String.IsNullOrEmpty(access) ||
      String.IsNullOrEmpty(subjectType) ||
      String.IsNullOrEmpty(subjectId)) {
      return this.Unauthorized(new {
        Message = "Required claims are missing."
      });
    }

    var scopedPermissions = ScopedPermission.Parse(access);

    if (subjectType.Equals("ApiKey", StringComparison.OrdinalIgnoreCase)) {
      //subjectId is the apiKeyId
      var apiKey = await this._apiKeys.FindAsync(new Guid(subjectId), cancellationToken);
      var repository = await this._permissionsRepository.GetObjectsFromIds(null, scopedPermissions.EnvironmentId, scopedPermissions.TenantId, cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.OK,
        new List<PermissionConfiguration>() {
          new PermissionConfiguration(new Guid(), String.Empty, apiKey.Type, repository.environment?.Name, repository.tenant?.Name)
        });
    } else {
      //SubjectId is the userId
      return this.StatusCode((Int32)HttpStatusCode.OK,
        await this._permissionsRepository.GetPermissionsAsync(new Guid(subjectId), cancellationToken));
    }
  }

  /// <summary>
  /// Get permissions the current user has access to.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <response code="200">successfully retrieved permissions.</response>
  /// <response code="401">The user is not authorized to get permission.</response>
  [Authorize(Policy = "AllowAnyScope")]
  [HttpGet(Name = "GetPermissions")]
  [ProducesResponseType(typeof(PermissionConfiguration[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [ProducesResponseType(500)]
  public async Task<ActionResult> GetPermissions(
    CancellationToken cancellationToken = default) {

    //if SonarSubjectType == "ApiKey"  then SonarSubjectId == ApiKeyId
    //if SonarSubjectType == "Sso"  then SonarSubjectId == UserId
    var access = User.FindFirst(x => x.Type.Equals("SonarAccess", StringComparison.OrdinalIgnoreCase))?.Value;
    var subjectId = User.FindFirst(x => x.Type.Equals("SonarSubjectId", StringComparison.OrdinalIgnoreCase))?.Value;
    var subjectType = User.FindFirst(x => x.Type.Equals("SonarSubjectType", StringComparison.OrdinalIgnoreCase))?.Value;

    // return 401 (Unauthorized) if claims are missing
    if (String.IsNullOrEmpty(access) ||
      String.IsNullOrEmpty(subjectType) ||
      String.IsNullOrEmpty(subjectId)) {
      return this.Unauthorized(new {
        Message = "Required claims are missing."
      });
    }

    var scopedPermissions = ScopedPermission.Parse(access);

    if (this.User.HasGlobalAccess()) {
      var permConfig = await this._permissionsRepository.GetPermissionsAsync(cancellationToken);
      return this.StatusCode((Int32)HttpStatusCode.OK, permConfig);
    } else {
      if (subjectType.Equals("ApiKey", StringComparison.OrdinalIgnoreCase)) {
        //subjectId is the apiKeyId
        return this.StatusCode(
            (Int32)HttpStatusCode.OK,
            await this._permissionsRepository.GetPermissionsScopeAsync(scopedPermissions.EnvironmentId, scopedPermissions.TenantId, cancellationToken)
          );
      } else {
        //SubjectId is the userId
        return this.StatusCode(
          (Int32)HttpStatusCode.OK,
          await this._permissionsRepository.GetPermissionsScopeAsync(new Guid(subjectId), cancellationToken)
          );
      }
    }
  }
  private static void ValidatePermission(
      ClaimsPrincipal principal,
      Guid? environmentScope,
      Guid? tenantScope,
      String activity) {

    // Make sure that the Api Key being created is within a scope that the API client has Admin access to
    if (environmentScope.HasValue) {
      if (tenantScope.HasValue) {
        if (!principal.HasTenantAccess(environmentScope.Value, tenantScope.Value, PermissionType.Admin)) {
          throw new ForbiddenException(
            $"Not authorized to {activity}, because it has a different Tenant scope."
          );
        }
      } else if (!principal.HasEnvironmentAccess(environmentScope.Value, PermissionType.Admin)) {
        throw new ForbiddenException(
          $"Not authorized to {activity} because it has a different Environment scope."
        );
      }
    } else {
      //If scope of work requires global admin, make sure authenticated key has global admin scope.
      if (!principal.HasGlobalAccess(PermissionType.Admin)) {
        throw new ForbiddenException(
          $"Not authorized to {activity}, must have global Admin scope.");
      }
    }
  }

  private static void ValidatePermissionDetails(PermissionDetails permissionDetails, User? user, Environment? environment, Tenant? tenant) {

    //User is a required field
    if (String.IsNullOrEmpty(permissionDetails.UserEmail) || (user == null)) {
      throw new BadRequestException($"user missing or does not exist {permissionDetails.UserEmail}");
    }
    if (!String.IsNullOrEmpty(permissionDetails.Environment) && (environment == null)) {
      throw new BadRequestException("environment name not found");
    }
    if (!String.IsNullOrEmpty(permissionDetails.Tenant) && (tenant == null)) {
      throw new BadRequestException("tenant name not found");
    }
    if ((environment == null) && (tenant != null)) {
      throw new BadRequestException("Need to specify the environment name that the tenant belongs to");
    }

  }

  public static PermissionConfiguration ToPermissionConfig(
    Environment? environment,
    Tenant? tenant,
    String email,
    UserPermission up) {

    return new PermissionConfiguration(
      up.Id,
      userEmail: email,
      up.Permission,
      environment?.Name,
      tenant?.Name
    );
  }
}
