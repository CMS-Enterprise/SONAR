using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/environments")]
public class EnvironmentController : ControllerBase {
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;

  public EnvironmentController(
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper) {
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
  }

  [HttpGet(Name = "GetEnvironments")]
  [ProducesResponseType(typeof(EnvironmentHealth[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetEnvironments(
    CancellationToken cancellationToken = default) {
    var environmentList = new List<EnvironmentHealth>();
    var environments = await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var environment in environments) {
      var tenantsHealth = await this._tenantDataHelper.GetTenantsHealth(environment, cancellationToken);
      environmentList.Add(this.ToEnvironmentHealth(tenantsHealth, environment));
    }

    return this.Ok(environmentList);
  }

  private EnvironmentHealth ToEnvironmentHealth(
    IList<TenantHealth> tenantsHealths,
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
