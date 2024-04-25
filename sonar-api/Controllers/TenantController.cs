using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/tenants")]
public class TenantController : ControllerBase {
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly EnvironmentDataHelper _environmentDataHelper;

  public TenantController(
    TenantDataHelper tenantDataHelper,
    EnvironmentDataHelper environmentDataHelper) {
    this._tenantDataHelper = tenantDataHelper;
    this._environmentDataHelper = environmentDataHelper;
  }

  /// <summary>
  ///   Fetch tenant health. Query parameters may be supplied to query by environment and/or tenant. This endpoint
  ///   will return all tenants for all environments if query params aren't supplied.
  /// </summary>
  [HttpGet(Name = "GetTenants")]
  [ProducesResponseType(typeof(TenantInfo[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetTenants(
    [FromQuery(Name = "environment")] String environmentName = "",
    [FromQuery(Name = "tenant")] String tenantName = "",
    CancellationToken cancellationToken = default) {

    if ((!environmentName.IsNullOrEmpty()) || (!tenantName.IsNullOrEmpty())) {
      return await GetTenantDetails(environmentName, tenantName, cancellationToken);
    }

    var tenantList = new List<TenantInfo>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var e in environments) {
      var res = await this._tenantDataHelper.GetTenantsInfo(e, tenantName, cancellationToken);
      tenantList.AddRange(res);
    }

    return this.Ok(tenantList);
  }

  /// <summary>
  ///   Fetch a list of tenants without health data
  /// </summary>
  [HttpGet("view", Name = "GetTenantsView")]
  [ProducesResponseType(typeof(TenantInfo[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetTenantsView(
    [FromQuery(Name = "environment")] String environmentName = "",
    [FromQuery(Name = "tenant")] String tenantName = "",
    CancellationToken cancellationToken = default) {

    if ((!environmentName.IsNullOrEmpty()) || (!tenantName.IsNullOrEmpty())) {
      var env = await this._environmentDataHelper
        .FetchExistingEnvAsync(environmentName, cancellationToken);
      var tenants = await this._tenantDataHelper
        .GetTenantsView(env, tenantName, cancellationToken);

      // Throw not found exception if specific tenant requested but none found
      if (!String.IsNullOrEmpty(tenantName) && tenants.Count == 0) {
        throw new ResourceNotFoundException(nameof(Tenant), tenantName);
      }

      return this.Ok(tenants);
    }

    var tenantList = new List<TenantInfo>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var e in environments) {
      var res = await this._tenantDataHelper.GetTenantsView(e, null, cancellationToken);
      tenantList.AddRange(res);
    }

    return this.Ok(tenantList);
  }

  private async Task<ActionResult> GetTenantDetails(String environmentName, String tenantName, CancellationToken cancelToken) {
    var env = await this._environmentDataHelper.FetchExistingEnvAsync(environmentName, cancelToken);
    var tenantsInfo = await this._tenantDataHelper.GetTenantsInfo(env, tenantName, cancelToken);

    // Throw not found exception if specific tenant requested but none found
    if (!String.IsNullOrEmpty(tenantName) && tenantsInfo.Count == 0) {
      throw new ResourceNotFoundException(nameof(Tenant), tenantName);
    }

    return this.Ok(tenantsInfo);
  }
}
