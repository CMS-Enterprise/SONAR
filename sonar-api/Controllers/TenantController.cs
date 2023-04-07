using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
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

  [HttpGet(Name = "GetTenants")]
  [ProducesResponseType(typeof(TenantHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetTenants(
    CancellationToken cancellationToken = default) {
    var tenantList = new List<TenantHealth>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var e in environments) {
      var res = await this._tenantDataHelper.GetTenantsHealth(e, cancellationToken);
      tenantList.AddRange(res);
    }

    return this.Ok(tenantList);
  }
}
