using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/environments")]
public class EnvironmentController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;

  public EnvironmentController(
    DataContext dbContext,
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper) {

    this._dbContext = dbContext;
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
  }

  [HttpPost(Name = "CreateEnvironment")]
  [Consumes(typeof(EnvironmentModel), contentType: "application/json")]
  [ProducesResponseType(typeof(EnvironmentModel), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 409)]
  [Authorize(Policy = "Admin")]
  public async Task<ActionResult> CreateEnvironment(
    [FromBody] EnvironmentModel environmentModel,
    CancellationToken cancellationToken) {

    await using var tx = await this._dbContext.Database.BeginTransactionAsync(cancellationToken);

    var existingEnv =
      await this._environmentDataHelper.TryFetchEnvironmentAsync(
        environmentModel.Name,
        cancellationToken
      );

    if (existingEnv != null) {
      return this.Conflict(new ProblemDetails {
        Title = $"The specified {nameof(Environment)} already exist."
      });
    }

    var entity =
      await this._environmentDataHelper.AddAsync(
        Environment.New(environmentModel.Name),
        cancellationToken
      );

    await this._dbContext.SaveChangesAsync(cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return this.Created(
      this.Url.Action(nameof(GetEnvironment), new { environment = entity.Name }) ?? "",
      new EnvironmentModel(entity.Name)
    );
  }

  /// <summary>
  ///   Fetch a list of all environments and their current sonar aggregate health status.
  /// </summary>
  [HttpGet(Name = "GetEnvironments")]
  [ProducesResponseType(typeof(EnvironmentHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [AllowAnonymous]
  public async Task<ActionResult> GetEnvironments(
    CancellationToken cancellationToken = default) {
    var environmentList = new List<EnvironmentHealth>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var environment in environments) {
      var tenantsHealth = await this._tenantDataHelper.GetTenantsInfo(environment, cancellationToken);
      environmentList.Add(ToEnvironmentHealth(tenantsHealth, environment));
    }

    return this.Ok(environmentList);
  }

  /// <summary>
  ///   Fetch a single environment's current sonar aggregate health status.
  /// </summary>
  /// <param name="environment">Environment name that the user is querying.</param>
  /// <param name="cancellationToken"></param>
  [HttpGet("{environment}", Name = "GetEnvironment")]
  [ProducesResponseType(typeof(EnvironmentHealth), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [AllowAnonymous]
  public async Task<ActionResult> GetEnvironment(
    [FromRoute]
    String environment,
    CancellationToken cancellationToken) {

    var existing =
      await this._environmentDataHelper.FetchExistingEnvAsync(environment, cancellationToken);

    var tenantsHealth = await this._tenantDataHelper.GetTenantsInfo(existing, cancellationToken);
    return this.Ok(ToEnvironmentHealth(tenantsHealth, existing));
  }

  [HttpDelete("{environment}", Name = "DeleteEnvironment")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [Authorize(Policy = "Admin")]
  public async Task<ActionResult> DeleteEnvironment(
    [FromRoute]
    String environment,
    CancellationToken cancellationToken) {

    await using var tx = await this._dbContext.Database.BeginTransactionAsync(cancellationToken);

    var existing =
      await this._environmentDataHelper.FetchExistingEnvAsync(environment, cancellationToken);

    var tenants =
      await this._tenantDataHelper.ListTenantsForEnvironment(existing.Id, cancellationToken);

    if (tenants.Any()) {
      throw new BadRequestException(
        $"Unable to delete the specified {nameof(Environment)} because there are tenants that must be deleted first.",
        ProblemTypes.DependentResourcesPreventDeletion
      );
    }

    this._environmentDataHelper.Delete(existing);
    await this._dbContext.SaveChangesAsync(cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return this.NoContent();
  }

  private static EnvironmentHealth ToEnvironmentHealth(
    IList<TenantInfo> tenantsHealths,
    Environment environment
  ) {
    HealthStatus? aggregateStatus = HealthStatus.Unknown;
    DateTime? statusTimestamp = null;

    foreach (var tenantHealth in tenantsHealths) {
      if (tenantHealth.AggregateStatus.HasValue) {
        if ((aggregateStatus == null) ||
          (aggregateStatus < tenantHealth.AggregateStatus) ||
          (tenantHealth.AggregateStatus == HealthStatus.Unknown)) {
          aggregateStatus = tenantHealth.AggregateStatus;
        }

        // The child service should always have a timestamp here, but double check anyway
        if (tenantHealth.Timestamp.HasValue &&
          (!statusTimestamp.HasValue || (tenantHealth.Timestamp.Value < statusTimestamp.Value))) {
          // The status timestamp should always be the *oldest* of the
          // recorded status data points.
          statusTimestamp = tenantHealth.Timestamp.Value;
        }
      } else {
        // One of the child services has an "unknown" status, that means
        // this service will also have the "unknown" status.
        aggregateStatus = null;
        statusTimestamp = null;
        break;
      }
    }

    return new EnvironmentHealth(environment.Name, statusTimestamp, aggregateStatus);
  }
}
