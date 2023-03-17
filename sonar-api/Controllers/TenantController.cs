using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[Route("api/v2/tenants")]
public class TenantController : ControllerBase {
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly EnvironmentDataHelper _environmentDataHelper;

  public TenantController(
    TenantDataHelper tenantDataHelper,
    EnvironmentDataHelper environmentDataHelper) {
    this._tenantDataHelper = tenantDataHelper;
    this._environmentDataHelper = environmentDataHelper;
  }

  [HttpGet]
  [ProducesResponseType(typeof(Tenant), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetTenants(
    CancellationToken cancellationToken = default) {
    IList<TenantHealth> tenantList = new List<TenantHealth>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var e in environments) {
      tenantList = await this._tenantDataHelper.GetTenantsHealth(e, cancellationToken);
    }

    return this.Ok(tenantList);
  }
}



