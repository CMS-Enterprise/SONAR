using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Prometheus;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/environments")]
public class EnvironmentController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly MaintenanceDataHelper<ScheduledEnvironmentMaintenance> _scheduledEnvironmentMaintenanceDataHelper;
  private readonly IPrometheusService _prometheusService;
  public EnvironmentController(
    DataContext dbContext,
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper,
    MaintenanceDataHelper<ScheduledEnvironmentMaintenance> scheduledEnvironmentMaintenanceDataHelper,
    IPrometheusService prometheusService) {

    this._dbContext = dbContext;
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._scheduledEnvironmentMaintenanceDataHelper = scheduledEnvironmentMaintenanceDataHelper;
    this._prometheusService = prometheusService;
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
        Environment.New(environmentModel.Name, environmentModel.IsNonProd),
        cancellationToken
      );

    var createdEnvMaintenances = await this.AddEnvironmentMaintenance(entity, environmentModel, cancellationToken);

    await this._dbContext.SaveChangesAsync(cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return this.Created(
         this.Url.Action(nameof(GetEnvironment), new { environment = entity.Name }) ?? "",
         new EnvironmentModel(
           name: entity.Name,
           isNonProd: entity.IsNonProd,
           createdEnvMaintenances.Select(e => new ScheduledMaintenanceConfiguration(
             e.ScheduleExpression,
             e.DurationMinutes,
             e.ScheduleTimeZone
           )).ToImmutableList())
       );
  }

  /// <summary>
  ///   Update environment.
  /// </summary>
  /// <param name="environment">The name of the environment updating.</param>
  /// <param name="environmentModel">The body contains Json of type Environment Model.  The Name is required but is not used or validated.  The environment name from the address route(URL) is used.</param>
  /// <param name="cancellationToken"></param>
  /// <response code="200">The environment has been updated.</response>
  /// <response code="401">The API key in the header is not authorized for updating a tenant.</response>
  /// <response code="404">The specified environment was not found.</response>
  [HttpPut("{environment}", Name = "UpdateEnvironment")]
  [Consumes(typeof(EnvironmentModel), contentType: "application/json")]
  [ProducesResponseType(typeof(EnvironmentModel), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> UpdateEnvironment(
    [FromRoute] String environment,
    [FromBody] EnvironmentModel environmentModel,
    CancellationToken cancellationToken = default) {

    await using var tx = await this._dbContext.Database.BeginTransactionAsync(cancellationToken);

    var existingEnv =
      await this._environmentDataHelper.TryFetchEnvironmentAsync(
        environment,
        cancellationToken
      );

    if (existingEnv == null) {
      throw new ResourceNotFoundException(nameof(Environment), environment);
    }

    existingEnv.IsNonProd = environmentModel.IsNonProd;

    var entity = await this._environmentDataHelper.Update(existingEnv);

    await this._dbContext.SaveChangesAsync(cancellationToken);

    // Remove existing scheduled maintenance
    await this._scheduledEnvironmentMaintenanceDataHelper
      .ExecuteDeleteByAssocEntityIdAsync(existingEnv.Id, cancellationToken);

    // add the new schedule maintenance to the database
    var updatedScheduledMaintenances = await this.AddEnvironmentMaintenance(entity, environmentModel, cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return this.Ok(
      new EnvironmentModel(
        existingEnv.Name,
        existingEnv.IsNonProd,
        updatedScheduledMaintenances.Select(e => new ScheduledMaintenanceConfiguration(
          e.ScheduleExpression,
          e.DurationMinutes,
          e.ScheduleTimeZone
          )).ToImmutableList())
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
      var tenantsHealth = await this._tenantDataHelper.GetTenantsInfo(environment, null, cancellationToken);
      var (isInMaintenance, maintenanceTypes) = await this._prometheusService.GetScopedCurrentMaintenanceStatus(
        environment: environment.Name,
        tenant: null,
        service: null,
        scope: MaintenanceScope.Environment,
        cancellationToken: cancellationToken);

      environmentList.Add(ToEnvironmentHealth(tenantsHealth, environment, isInMaintenance, maintenanceTypes));
    }

    return this.Ok(environmentList);
  }

  /// <summary>
  ///   Fetch a list of all environments without health data
  /// </summary>
  [HttpGet("view", Name = "GetEnvironmentsView")]
  [ProducesResponseType(typeof(EnvironmentModel[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [AllowAnonymous]
  public async Task<ActionResult> GetEnvironmentsView(
    CancellationToken cancellationToken = default) {
    var environmentList = new List<EnvironmentModel>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    // populate scheduled maintenance for each environment
    foreach (var environment in environments) {
      ImmutableList<ScheduledMaintenanceConfiguration>? scheduledMaintenanceConfig = null;
      var scheduledMaintenance =
        await this._scheduledEnvironmentMaintenanceDataHelper.SingleOrDefaultByAssocEntityIdAsync(environment.Id,
          cancellationToken);
      if (scheduledMaintenance != null) {
        scheduledMaintenanceConfig = ImmutableList<ScheduledMaintenanceConfiguration>.Empty.Add(
          new ScheduledMaintenanceConfiguration(
            scheduledMaintenance.ScheduleExpression,
            scheduledMaintenance.DurationMinutes,
            scheduledMaintenance.ScheduleTimeZone));
      }
      environmentList.Add(new EnvironmentModel(
        environment.Name,
        environment.IsNonProd,
        scheduledMaintenanceConfig));
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

    var tenantsHealth = await this._tenantDataHelper.GetTenantsInfo(existing, null, cancellationToken);
    var (isInMaintenance, maintenanceTypes) = await this._prometheusService.GetScopedCurrentMaintenanceStatus(
        environment: environment,
        tenant: null,
        service: null,
        scope: MaintenanceScope.Environment,
        cancellationToken: cancellationToken);

    return this.Ok(ToEnvironmentHealth(tenantsHealth, existing, isInMaintenance, maintenanceTypes));
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
    Environment environment,
    Boolean isInMaintenance,
    String? maintenanceTypes
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

    return new EnvironmentHealth(
      environment.Name,
      statusTimestamp,
      aggregateStatus,
      environment.IsNonProd,
      isInMaintenance,
      maintenanceTypes);
  }

  private static ScheduledEnvironmentMaintenance ToScheduledEnvironmentMaintenance(
    Environment environment,
    ScheduledMaintenanceConfiguration configModel) {
    return ScheduledEnvironmentMaintenance.New(
      scheduleExpression: configModel.ScheduleExpression,
      scheduleTimeZone: configModel.ScheduleTimeZone,
      durationMinutes: configModel.DurationMinutes,
      environmentId: environment.Id
    );
  }

  private async Task<IImmutableList<ScheduledEnvironmentMaintenance>> AddEnvironmentMaintenance(
    Environment entity,
    EnvironmentModel environmentModel,
    CancellationToken cancellationToken) {
    var scheduledEnvironmentMaintenances = environmentModel.ScheduledMaintenances?
        .Select(c => ToScheduledEnvironmentMaintenance(entity, c))
      ?? Enumerable.Empty<ScheduledEnvironmentMaintenance>();

    var createdEnvMaintenances = await this._scheduledEnvironmentMaintenanceDataHelper.AddAllAsync(
      scheduledEnvironmentMaintenances,
      cancellationToken
    );

    return createdEnvMaintenances;
  }
}
