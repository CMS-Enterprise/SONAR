using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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
      var res = await this._tenantDataHelper.GetTenantsInfo(e, cancellationToken);
      tenantList.AddRange(res);
    }

    return this.Ok(tenantList);
  }

  private async Task<ActionResult> GetTenantDetails(String environmentName, String tenantName, CancellationToken cancelToken) {
    var tenantList = new List<TenantInfo>();

    var env = await this._environmentDataHelper.FetchExistingEnvAsync(environmentName, cancelToken);
    var tenantsInfo = await this._tenantDataHelper.GetTenantsInfo(env, cancelToken);

    if (tenantName.IsNullOrEmpty()) {
      //Get all tenants details for the environment
      tenantList = tenantsInfo as List<TenantInfo>;
    } else {
      //Get a single tenant details for the environment
      var tenantInfo = tenantsInfo.FirstOrDefault(t => t.TenantName == tenantName);
      if (tenantInfo != null) {
        tenantList.Add(tenantInfo);
      } else {
        throw new ResourceNotFoundException(nameof(Tenant), tenantName);
      }
    }
    return this.Ok(tenantList);
  }
}
