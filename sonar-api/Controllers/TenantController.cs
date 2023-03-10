using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/tenants")]
public class TenantController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<HealthCheck> _healthsTable;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly ApiKeyDataHelper _apiKeyDataHelper;

  public TenantController(
    DataContext dbContext,
    DbSet<Tenant> tenantsTable,
    DbSet<HealthCheck> healthsTable,
    ServiceDataHelper serviceDataHelper,
    TenantDataHelper tenantDataHelper,
    ApiKeyDataHelper apiKeyDataHelper) {
    this._dbContext = dbContext;
    this._tenantsTable = tenantsTable;
    this._healthsTable = healthsTable;
    this._serviceDataHelper = serviceDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._apiKeyDataHelper = apiKeyDataHelper;

  }
  /*
   * When listing tenants  it would be good to get the aggregate health status
   * for just the root services (no details on child services or
   * individual health checks).
   */

  [HttpGet]
  [ProducesResponseType(typeof(Tenant), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetTenants(
    CancellationToken cancellationToken = default) {
    //var tenantsList = await this._tenantDataHelper.FetchAllExistingTenantsAsync(
    //  cancellationToken);

    //Get all Environments


    //var tenant = this._serviceDataHelper.FetchExistingConfiguration("foo", "baz", cancellationToken);
    var tenant = await this._tenantDataHelper.FetchAllExistingTenantsAsync(cancellationToken);

    return this.Ok(tenant);
  }
}
