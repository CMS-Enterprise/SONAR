using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Environment = System.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/maintenance")]
public class MaintenanceController : ControllerBase {
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly UserDataHelper _userDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly MaintenanceDataHelper<AdHocEnvironmentMaintenance> _adHocEnvironmentMaintenanceDataHelper;
  private readonly MaintenanceDataHelper<AdHocTenantMaintenance> _adHocTenantMaintenanceDataHelper;
  private readonly MaintenanceDataHelper<AdHocServiceMaintenance> _adHocServiceMaintenanceDataHelper;
  private readonly MaintenanceDataHelper<ScheduledEnvironmentMaintenance> _scheduledEnvironmentMaintenanceDataHelper;
  private readonly MaintenanceDataHelper<ScheduledTenantMaintenance> _scheduledTenantMaintenanceDataHelper;
  private readonly MaintenanceDataHelper<ScheduledServiceMaintenance> _scheduledServiceMaintenanceDataHelper;

  public MaintenanceController(
    EnvironmentDataHelper environmentDataHelper,
    UserDataHelper userDataHelper,
    TenantDataHelper tenantDataHelper,
    ServiceDataHelper serviceDataHelper,
    MaintenanceDataHelper<AdHocEnvironmentMaintenance> adHocEnvironmentMaintenanceDataHelper,
    MaintenanceDataHelper<AdHocTenantMaintenance> adHocTenantMaintenanceDataHelper,
    MaintenanceDataHelper<AdHocServiceMaintenance> adHocServiceMaintenanceDataHelper,
    MaintenanceDataHelper<ScheduledEnvironmentMaintenance> scheduledEnvironmentMaintenanceDataHelper,
    MaintenanceDataHelper<ScheduledTenantMaintenance> scheduledTenantMaintenanceDataHelper,
    MaintenanceDataHelper<ScheduledServiceMaintenance> scheduledServiceMaintenanceDataHelper) {

    this._environmentDataHelper = environmentDataHelper;
    this._userDataHelper = userDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._adHocEnvironmentMaintenanceDataHelper = adHocEnvironmentMaintenanceDataHelper;
    this._adHocTenantMaintenanceDataHelper = adHocTenantMaintenanceDataHelper;
    this._adHocServiceMaintenanceDataHelper = adHocServiceMaintenanceDataHelper;
    this._scheduledEnvironmentMaintenanceDataHelper = scheduledEnvironmentMaintenanceDataHelper;
    this._scheduledTenantMaintenanceDataHelper = scheduledTenantMaintenanceDataHelper;
    this._scheduledServiceMaintenanceDataHelper = scheduledServiceMaintenanceDataHelper;
  }

  [AllowAnonymous]
  [HttpGet("environments/scheduled", Name = "GetActiveScheduledEnvironmentMaintenance")]
  [ProducesResponseType(typeof(ActiveScheduledMaintenanceView[]), statusCode: 200)]
  public async Task<IActionResult> GetActiveScheduledEnvironmentMaintenances(CancellationToken cancellationToken = default) {
    var environmentMaintenances = await this._scheduledEnvironmentMaintenanceDataHelper.FindAllAsync(cancellationToken);
    var envs = await this._environmentDataHelper
      .FetchByEnvIdsAsync(
        environmentMaintenances
          .Select(e => e.EnvironmentId).ToList(),
        cancellationToken);

    return this.Ok(
      environmentMaintenances.Select(r =>
        new ActiveScheduledMaintenanceView(
          id: r.Id,
          scope: MaintenanceScope.Environment,
          environment: envs.Single(e => e.Id == r.EnvironmentId).Name,
          tenant: null,
          service: null,
          scheduleExpression: r.ScheduleExpression,
          duration: r.DurationMinutes,
          timeZone: r.ScheduleTimeZone)));
  }

  [AllowAnonymous]
  [HttpGet("tenants/scheduled", Name = "GetActiveScheduledTenantMaintenance")]
  [ProducesResponseType(typeof(ActiveScheduledMaintenanceView[]), statusCode: 200)]
  public async Task<IActionResult> GetActiveScheduledTenantMaintenances(CancellationToken cancellationToken = default) {
    var tenantMaintenances = await this._scheduledTenantMaintenanceDataHelper.FindAllAsync(cancellationToken);
    var tenants = await this._tenantDataHelper
      .FetchByTenantIdsAsync(
        tenantMaintenances
          .Select(e => e.TenantId).ToList(),
        cancellationToken);
    var environments = await this._environmentDataHelper.FetchByEnvIdsAsync(
      tenants
        .Select(t => t.EnvironmentId).Distinct().ToList(),
      cancellationToken);

    return this.Ok(
      tenantMaintenances.Select(r => {
        var tenant = tenants.Single(e => e.Id == r.TenantId);
        var env = environments.Single(e => e.Id == tenant.EnvironmentId);
        return new ActiveScheduledMaintenanceView(
          id: r.Id,
          scope: MaintenanceScope.Tenant,
          environment: env.Name,
          tenant: tenant.Name,
          service: null,
          scheduleExpression: r.ScheduleExpression,
          duration: r.DurationMinutes,
          timeZone: r.ScheduleTimeZone);
      }));
  }

  [AllowAnonymous]
  [HttpGet("services/scheduled", Name = "GetActiveScheduledServiceMaintenance")]
  [ProducesResponseType(typeof(ActiveScheduledMaintenanceView[]), statusCode: 200)]
  public async Task<IActionResult> GetActiveScheduledServiceMaintenances(CancellationToken cancellationToken = default) {
    var serviceMaintenances = await this._scheduledServiceMaintenanceDataHelper.FindAllAsync(cancellationToken);
    var services = await this._serviceDataHelper
      .FetchByServiceIdsAsync(
        serviceMaintenances
          .Select(e => e.ServiceId).ToList(),
        cancellationToken);
    var tenants = await this._tenantDataHelper
          .FetchByTenantIdsAsync(
            services
              .Select(e => e.TenantId).Distinct().ToList(),
            cancellationToken);
    var environments = await this._environmentDataHelper
      .FetchByEnvIdsAsync(
        tenants
          .Select(e => e.EnvironmentId).Distinct().ToList(),
        cancellationToken);

    return this.Ok(
      serviceMaintenances.Select(r => {
        var service = services.Single(e => e.Id == r.ServiceId);
        var tenant = tenants.Single(t => t.Id == service?.TenantId);
        var environment = environments.Single(e => e.Id == tenant?.EnvironmentId);

        return new ActiveScheduledMaintenanceView(
          id: r.Id,
          scope: MaintenanceScope.Service,
          environment: environment.Name,
          tenant: tenant.Name,
          service: service.Name,
          scheduleExpression: r.ScheduleExpression,
          duration: r.DurationMinutes,
          timeZone: r.ScheduleTimeZone);
      }));
  }

  [AllowAnonymous]
  [HttpGet("environments/ad-hoc", Name = "GetActiveAdHocEnvironmentMaintenance")]
  [ProducesResponseType(typeof(ActiveAdHocMaintenanceView[]), statusCode: 200)]
  public async Task<IActionResult> GetActiveAdHocEnvironmentMaintenances(CancellationToken cancellationToken = default) {
    var environmentMaintenances = await this._adHocEnvironmentMaintenanceDataHelper.FindAllAsync(cancellationToken);
    var envs = await this._environmentDataHelper
      .FetchByEnvIdsAsync(
        environmentMaintenances
          .Select(e => e.EnvironmentId).ToList(),
        cancellationToken);

    var users = await this._userDataHelper.FetchByUserIdsAsync(
      environmentMaintenances
        .Select(e => e.AppliedByUserId).ToList(),
      cancellationToken);

    return this.Ok(
      environmentMaintenances.Select(r =>
        new ActiveAdHocMaintenanceView(
          id: r.Id,
          scope: MaintenanceScope.Environment,
          environment: envs.Single(e => e.Id == r.EnvironmentId).Name,
          tenant: null,
          service: null,
          appliedByUserName: users.Single(u => u.Id == r.AppliedByUserId).FullName,
          startTime: r.StartTime,
          endTime: r.EndTime)));
  }

  [AllowAnonymous]
  [HttpGet("tenants/ad-hoc", Name = "GetActiveAdHocTenantMaintenance")]
  [ProducesResponseType(typeof(ActiveAdHocMaintenanceView[]), statusCode: 200)]
  public async Task<IActionResult> GetActiveTenantAdHocMaintenances(CancellationToken cancellationToken = default) {
    var tenantMaintenances = await this._adHocTenantMaintenanceDataHelper.FindAllAsync(cancellationToken);
    var tenants = await this._tenantDataHelper
      .FetchByTenantIdsAsync(
        tenantMaintenances
          .Select(e => e.TenantId).ToList(),
        cancellationToken);
    var environments = await this._environmentDataHelper.FetchByEnvIdsAsync(
      tenants
        .Select(t => t.EnvironmentId).Distinct().ToList(),
      cancellationToken);

    var users = await this._userDataHelper.FetchByUserIdsAsync(
      tenantMaintenances
        .Select(e => e.AppliedByUserId).ToList(),
      cancellationToken);

    return this.Ok(
      tenantMaintenances.Select(r => {
        var tenant = tenants.Single(e => e.Id == r.TenantId);
        var env = environments.Single(e => e.Id == tenant.EnvironmentId);
        return new ActiveAdHocMaintenanceView(
          id: r.Id,
          scope: MaintenanceScope.Tenant,
          environment: env.Name,
          tenant: tenant.Name,
          service: null,
          appliedByUserName: users.Single(u => u.Id == r.AppliedByUserId).FullName,
          startTime: r.StartTime,
          endTime: r.EndTime);
      }));
  }

  [AllowAnonymous]
  [HttpGet("services/ad-hoc", Name = "GetActiveAdHocServiceMaintenance")]
  [ProducesResponseType(typeof(ActiveAdHocMaintenanceView[]), statusCode: 200)]
  public async Task<IActionResult> GetActiveServiceAdHocMaintenances(CancellationToken cancellationToken = default) {
    var serviceMaintenances = await this._adHocServiceMaintenanceDataHelper.FindAllAsync(cancellationToken);
    var services = await this._serviceDataHelper
      .FetchByServiceIdsAsync(
        serviceMaintenances
          .Select(e => e.ServiceId).ToList(),
        cancellationToken);
    var tenants = await this._tenantDataHelper
          .FetchByTenantIdsAsync(
            services
              .Select(e => e.TenantId).Distinct().ToList(),
            cancellationToken);
    var environments = await this._environmentDataHelper
      .FetchByEnvIdsAsync(
        tenants
          .Select(e => e.EnvironmentId).Distinct().ToList(),
        cancellationToken);

    var users = await this._userDataHelper.FetchByUserIdsAsync(
      serviceMaintenances
        .Select(e => e.AppliedByUserId).ToList(),
      cancellationToken);

    return this.Ok(
      serviceMaintenances.Select(r => {
        var service = services.Single(e => e.Id == r.ServiceId);
        var tenant = tenants.Single(t => t.Id == service?.TenantId);
        var environment = environments.Single(e => e.Id == tenant?.EnvironmentId);

        return new ActiveAdHocMaintenanceView(
          id: r.Id,
          scope: MaintenanceScope.Service,
          environment: environment.Name,
          tenant: tenant.Name,
          service: service.Name,
          appliedByUserName: users.Single(u => u.Id == r.AppliedByUserId).FullName,
          startTime: r.StartTime,
          endTime: r.EndTime);
      }));
  }

  [HttpPut("{environment}/ad-hoc", Name = "ToggleAdhocEnvironmentMaintenance")]
  [Consumes(typeof(AdHocMaintenanceConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ActiveAdHocMaintenanceView), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 409)]
  public async Task<IActionResult> ToggleEnvironmentAdhocMaintenance(
    [FromRoute] String environment,
    [FromBody] AdHocMaintenanceConfiguration maintenanceConfig,
    CancellationToken cancellationToken = default) {

    var activity = $"toggle ad-hoc maintenance for the environment {environment}";

    var existingUser = await this.ValidateCurrentUserClaims(HttpContext.User, cancellationToken);
    var existingEnv =
      await this._environmentDataHelper.FetchExistingEnvAsync(
        environment,
        cancellationToken
      );

    // Check user permissions for environment
    ValidationHelper.ValidatePermissionScope(
      principal: this.User,
      environmentScope: existingEnv.Id,
      tenantScope: null,
      action: activity);

    // create new ad-hoc maintenance
    if (maintenanceConfig.IsEnabled) {
      try {
        var createdEntity = await this._adHocEnvironmentMaintenanceDataHelper.AddAsync(
          AdHocEnvironmentMaintenance.New(
            existingUser.Id,
            DateTime.UtcNow,
            maintenanceConfig.EndTime,
            existingEnv.Id),
          cancellationToken);

        return this.StatusCode(
          (Int32)HttpStatusCode.Created,
          new ActiveAdHocMaintenanceView(
            id: createdEntity.Id,
            scope: MaintenanceScope.Environment,
            environment: existingEnv.Name,
            tenant: null,
            service: null,
            appliedByUserName: existingUser.FullName,
            startTime: createdEntity.StartTime,
            endTime: createdEntity.EndTime)
        );
      } catch (DbUpdateException e) when (e.InnerException is PostgresException {
        SqlState: "23505",
        ConstraintName: "ix_ad_hoc_maintenance_environment_id"
      }) {
        return this.Conflict(new ProblemDetails {
          Title = $"Ad-hoc maintenance for {nameof(Environment)}: {existingEnv.Name} already exists."
        });
      }
    }

    // maintenance is not enabled, remove any existing maintenance
    await this._adHocEnvironmentMaintenanceDataHelper
      .ExecuteDeleteByAssocEntityIdAsync(existingEnv.Id, cancellationToken);

    return this.Ok();
  }

  [HttpPut("{environment}/tenants/{tenant}/ad-hoc", Name = "ToggleAdhocTenantMaintenance")]
  [Consumes(typeof(AdHocMaintenanceConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ActiveAdHocMaintenanceView), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 409)]
  public async Task<IActionResult> ToggleTenantAdhocMaintenance(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromBody] AdHocMaintenanceConfiguration maintenanceConfig,
    CancellationToken cancellationToken = default) {

    var activity = $"toggle ad-hoc maintenance for the tenant {tenant}";

    var existingUser = await this.ValidateCurrentUserClaims(HttpContext.User, cancellationToken);
    var existingEnvironment =
      await this._environmentDataHelper.FetchExistingEnvAsync(environment, cancellationToken);
    var existingTenant =
      await this._tenantDataHelper.FetchExistingTenantAsync(
        environment,
        tenant,
        cancellationToken
      );

    // Check user permissions for tenant
    ValidationHelper.ValidatePermissionScope(
      principal: this.User,
      environmentScope: existingEnvironment.Id,
      tenantScope: existingTenant.Id,
      action: activity);

    // create new ad-hoc maintenance
    if (maintenanceConfig.IsEnabled) {
      try {
        var createdEntity = await this._adHocTenantMaintenanceDataHelper.AddAsync(
          AdHocTenantMaintenance.New(
            existingUser.Id,
            DateTime.UtcNow,
            maintenanceConfig.EndTime,
            existingTenant.Id),
          cancellationToken);

        return this.StatusCode(
          (Int32)HttpStatusCode.Created,
          new ActiveAdHocMaintenanceView(
            id: createdEntity.Id,
            scope: MaintenanceScope.Tenant,
            environment: environment,
            tenant: existingTenant.Name,
            service: null,
            appliedByUserName: existingUser.FullName,
            startTime: createdEntity.StartTime,
            endTime: createdEntity.EndTime)
        );
      } catch (DbUpdateException e) when (e.InnerException is PostgresException {
        SqlState: "23505",
        ConstraintName: "ix_ad_hoc_maintenance_tenant_id"
      }) {
        return this.Conflict(new ProblemDetails {
          Title = $"Ad-hoc maintenance for {nameof(Tenant)}: {existingTenant.Name} already exists."
        });
      }
    }

    // maintenance is not enabled, remove any existing maintenance
    await this._adHocTenantMaintenanceDataHelper
      .ExecuteDeleteByAssocEntityIdAsync(existingTenant.Id, cancellationToken);

    return this.Ok();
  }

  [HttpPut("{environment}/tenants/{tenant}/services/{service}/ad-hoc", Name = "ToggleAdhocServiceMaintenance")]
  [Consumes(typeof(AdHocMaintenanceConfiguration), contentType: "application/json")]
  [ProducesResponseType(typeof(ActiveAdHocMaintenanceView), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 409)]
  public async Task<IActionResult> ToggleServiceAdhocMaintenance(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String service,
    [FromBody] AdHocMaintenanceConfiguration maintenanceConfig,
    CancellationToken cancellationToken = default) {

    var activity = $"toggle ad-hoc maintenance for the service {service}";

    var existingUser = await this.ValidateCurrentUserClaims(HttpContext.User, cancellationToken);
    var existingEnvironment =
      await this._environmentDataHelper.FetchExistingEnvAsync(environment, cancellationToken);
    var existingTenant =
      await this._tenantDataHelper.FetchExistingTenantAsync(environment, tenant, cancellationToken);
    var existingService =
      await this._serviceDataHelper.FetchExistingService(
        environment,
        tenant,
        service,
        cancellationToken
      );

    // Check user permissions for service
    ValidationHelper.ValidatePermissionScope(
      principal: this.User,
      environmentScope: existingEnvironment.Id,
      tenantScope: existingTenant.Id,
      action: activity);

    // create new ad-hoc maintenance
    if (maintenanceConfig.IsEnabled) {
      try {
        var createdEntity = await this._adHocServiceMaintenanceDataHelper.AddAsync(
          AdHocServiceMaintenance.New(
            existingUser.Id,
            DateTime.UtcNow,
            maintenanceConfig.EndTime,
            existingService.Id),
          cancellationToken);

        return this.StatusCode(
          (Int32)HttpStatusCode.Created,
          new ActiveAdHocMaintenanceView(
            id: createdEntity.Id,
            scope: MaintenanceScope.Service,
            environment: environment,
            tenant: tenant,
            service: existingService.Name,
            appliedByUserName: existingUser.FullName,
            startTime: createdEntity.StartTime,
            endTime: createdEntity.EndTime));
      } catch (DbUpdateException e) when (e.InnerException is PostgresException {
        SqlState: "23505",
        ConstraintName: "ix_ad_hoc_maintenance_service_id"
      }) {
        return this.Conflict(new ProblemDetails {
          Title = $"Ad-hoc maintenance for {nameof(Service)}: {existingService.Name} already exists."
        });
      }
    }

    // maintenance is not enabled, remove any existing maintenance
    await this._adHocServiceMaintenanceDataHelper
      .ExecuteDeleteByAssocEntityIdAsync(existingService.Id, cancellationToken);

    return this.Ok();
  }

  private async Task<User> ValidateCurrentUserClaims(ClaimsPrincipal? principal, CancellationToken cancellationToken) {
    if (principal == null) {
      throw new UnauthorizedException("Unauthorized to access requested resource.");
    }

    var userEmail = principal.FindFirstValue(ClaimTypes.Email);
    // return 400 (Bad Request) if claims are missing
    if (String.IsNullOrEmpty(userEmail)) {
      throw new BadRequestException("Required claims are missing");
    }

    var existingUser = await this._userDataHelper.FetchUserByEmailAsync(userEmail, cancellationToken);
    if (existingUser == null) {
      throw new UnauthorizedException("Unauthorized. Current user does not exist.");
    }

    return existingUser;
  }
}
