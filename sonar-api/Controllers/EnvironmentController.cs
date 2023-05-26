using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Route("api/v{version:apiVersion}/environments")]
public class EnvironmentController : ControllerBase {
  private readonly DataContext _dbContext;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;

  public EnvironmentController(
    DataContext dbContext,
    DbSet<Environment> environmentsTable,
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper) {

    this._dbContext = dbContext;
    this._environmentsTable = environmentsTable;
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
  }

  [HttpPost(Name = "CreateEnvironment")]
  [Consumes(typeof(EnvironmentModel), contentType: "application/json")]
  [ProducesResponseType(typeof(EnvironmentModel), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 409)]
  public async Task<ActionResult> CreateEnvironment(
    [FromBody] EnvironmentModel environmentModel,
    CancellationToken cancellationToken) {

    await using var tx = await this._dbContext.Database.BeginTransactionAsync(cancellationToken);

    var existingEnv = await
      this._environmentsTable
        .Where(e => e.Name == environmentModel.Name)
        .SingleOrDefaultAsync(cancellationToken);

    if (existingEnv != null) {
      return this.Conflict(new ProblemDetails {
        Title = $"The specified {nameof(Environment)} already exist."
      });
    }

    var entity =
      await this._environmentsTable.AddAsync(
        Environment.New(environmentModel.Name),
        cancellationToken
      );

    await this._dbContext.SaveChangesAsync(cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return this.Created(
      this.Url.Action(nameof(GetEnvironment), new { environment = entity.Entity.Name }) ?? "",
      new EnvironmentModel(entity.Entity.Name)
    );
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
      environmentList.Add(ToEnvironmentHealth(tenantsHealth, environment));
    }

    return this.Ok(environmentList);
  }

  [HttpGet("{environment}", Name = "GetEnvironment")]
  [ProducesResponseType(typeof(EnvironmentHealth), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> GetEnvironment(
    [FromRoute]
    String environment,
    CancellationToken cancellationToken) {

    var existing =
      await this._environmentDataHelper.FetchExistingEnvAsync(environment, cancellationToken);

    var tenantsHealth = await this._tenantDataHelper.GetTenantsHealth(existing, cancellationToken);
    return this.Ok(ToEnvironmentHealth(tenantsHealth, existing));
  }

  private static EnvironmentHealth ToEnvironmentHealth(
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
