using System;
using System.Collections.Generic;
using System.Net;
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
using Microsoft.Extensions.Logging;
using Environment = Cms.BatCave.Sonar.Data.Environment;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/error-reports")]
public class ErrorReportsController : ControllerBase {
  private readonly ILogger<ErrorReportsController> _logger;
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly ErrorReportsDataHelper _errorReportsDataHelper;

  public ErrorReportsController(
    ILogger<ErrorReportsController> logger,
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper,
    ServiceDataHelper serviceDataHelper,
    ErrorReportsDataHelper errorReportsDataHelper) {

    this._logger = logger;
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._serviceDataHelper = serviceDataHelper;
    this._errorReportsDataHelper = errorReportsDataHelper;
  }

  [HttpPost("{environment}", Name = "CreateErrorReport")]
  [Consumes(typeof(ErrorReportDetails), contentType: "application/json")]
  [ProducesResponseType(typeof(ErrorReportDetails), statusCode: 201)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 500)]
  public async Task<IActionResult> CreateErrorReport(
    [FromRoute] String environment,
    [FromBody] ErrorReportDetails reportDetails,
    CancellationToken cancellationToken = default) {

    // validate environment, tenant, service
    var validatedConfiguration = await ValidateReportData(
      environment,
      reportDetails.Tenant,
      reportDetails.Service,
      cancellationToken);

    // create error report
    var entity =
      await this._errorReportsDataHelper.AddErrorReportAsync(
        ErrorReport.New(
          reportDetails.Timestamp,
          validatedConfiguration.Environment.Id,
          validatedConfiguration.ExistingTenant?.Id,
          validatedConfiguration.ExistingService?.Name,
          reportDetails.HealthCheckName,
          reportDetails.Level,
          reportDetails.Type,
          reportDetails.Message,
          reportDetails.Configuration,
          reportDetails.StackTrace),
        cancellationToken);

    return this.StatusCode(
      (Int32)HttpStatusCode.Created,
      new ErrorReportDetails(
        entity.Timestamp,
        validatedConfiguration.ExistingTenant?.Name,
        entity.ServiceName,
        entity.HealthCheckName,
        entity.Level,
        entity.Type,
        entity.Message,
        entity.Configuration,
        entity.StackTrace));
  }

  [HttpGet("{environment}", Name = "ListErrorReport")]
  [ProducesResponseType(typeof(List<ErrorReportDetails>), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [ProducesResponseType(500)]
  public async Task<IActionResult> ListErrorReport(
    [FromRoute] String environment,
    [FromQuery] String? serviceName,
    [FromQuery] String? healthCheckName,
    [FromQuery] AgentErrorType? errorType,
    [FromQuery] DateTime? start,
    [FromQuery] DateTime? end,
    CancellationToken cancellationToken = default) {

    // validate environment
    var existingEnvironment = await this._environmentDataHelper
      .FetchExistingEnvAsync(environment, cancellationToken);

    var maxSpan = TimeSpan.FromDays(5);
    var defaultSpan = TimeSpan.FromDays(1);
    var now = DateTime.UtcNow;
    DateTime endVal = now;
    DateTime startVal = endVal.Subtract(defaultSpan);

    if (start != null && end == null) {
      startVal = DateTime.SpecifyKind((DateTime)start, DateTimeKind.Utc);
    } else if (start != null && end != null) {
      startVal = DateTime.SpecifyKind((DateTime)start, DateTimeKind.Utc);
      endVal = DateTime.SpecifyKind((DateTime)end, DateTimeKind.Utc);
    }

    if (DateTime.Compare(startVal, endVal) > 0) {
      throw new BadRequestException("Invalid Dates: Start date provided is after the end date.");
    }

    if ((endVal - startVal) > maxSpan) {
      throw new BadRequestException("Invalid Dates: Difference between timestamps exceeds max query span.");
    }

    var results = await this._errorReportsDataHelper.GetFilteredErrorReportDetailsByEnvironment(
      existingEnvironment.Id,
      serviceName,
      healthCheckName,
      errorType,
      startVal,
      endVal,
      cancellationToken);

    return this.Ok(results);
  }

  private async Task<(Environment Environment, Tenant? ExistingTenant, Service? ExistingService)> ValidateReportData(
    String environment,
    String? tenant,
    String? service,
    CancellationToken cancellationToken) {

    // validate env, tenant, and service as necessary
    var existingEnvironment = await this._environmentDataHelper
      .FetchExistingEnvAsync(environment, cancellationToken);

    Tenant? existingTenant = null;
    Service? existingService = null;

    if (!String.IsNullOrEmpty(tenant)) {
      existingTenant =
        await this._tenantDataHelper.FetchExistingTenantAsync(
          environment,
          tenant,
          cancellationToken);
    }

    if ((existingTenant != null) && !String.IsNullOrEmpty(service)) {
      existingService = await this._serviceDataHelper.FetchExistingService(
        environment,
        existingTenant.Name,
        service,
        cancellationToken);
    }

    return (existingEnvironment, existingTenant, existingService);
  }
}
