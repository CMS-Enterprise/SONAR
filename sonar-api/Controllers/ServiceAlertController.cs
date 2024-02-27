using System;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Alertmanager;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(2)]
[Authorize(Policy = "Admin")]
[Route("api/v{version:apiVersion}/alerts")]
public class ServiceAlertController : ControllerBase {

  private readonly ILogger<ServiceAlertController> _logger;
  private readonly ServiceDataHelper _serviceDataHelper;
  private readonly AlertingDataHelper _alertingDataHelper;
  private readonly IAlertmanagerService _alertmanagerService;

  public ServiceAlertController(
    ILogger<ServiceAlertController> logger,
    ServiceDataHelper serviceDataHelper,
    AlertingDataHelper alertingDataHelper,
    IAlertmanagerService alertmanagerService
  ) {
    this._logger = logger;
    this._serviceDataHelper = serviceDataHelper;
    this._alertingDataHelper = alertingDataHelper;
    this._alertmanagerService = alertmanagerService;
  }

  [HttpGet("{environment}/tenants/{tenant}/services/{*servicePath}", Name = "GetServiceAlerts")]
  [ProducesResponseType(typeof(ServiceAlert[]), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  [AllowAnonymous]
  public async Task<ActionResult> GetServiceAlerts(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    CancellationToken cancellationToken = default) {

    var serviceName = servicePath.Split('/').Last();

    var service = await this._serviceDataHelper.FetchExistingService(
      environment,
      tenant,
      serviceName,
      cancellationToken);

    var getActiveAlertsTask = this._alertmanagerService.GetActiveAlertsAsync(
      environment,
      tenant,
      serviceName,
      cancellationToken);

    var getServiceSilencesTask = this._alertmanagerService.GetActiveServiceSilencesAsync(
      environment,
      tenant,
      serviceName,
      cancellationToken);

    var alertingRules = await this._alertingDataHelper.FetchAlertingRulesAsync(service.Id, cancellationToken);

    var alertReceiversById =
      (await this._alertingDataHelper.FetchAlertReceiversAsync(service.TenantId, cancellationToken))
      .ToImmutableDictionary(keySelector: r => r.Id);

    try {
      await getActiveAlertsTask;
      await getServiceSilencesTask;
    } catch (TaskCanceledException e) {
      this._logger.LogError(exception: e, message: "Task cancelled while retrieving active alerts from Alertmanager.");
      return this.StatusCode(StatusCodes.Status504GatewayTimeout);
    }

    var activeAlertsByName = getActiveAlertsTask.Result
      .ToImmutableDictionary(keySelector: a => a.Labels[IAlertmanagerService.AlertNameLabel]);

    // fetch all silences for a service
    var serviceSilences = getServiceSilencesTask.Result;

    return this.Ok(
      alertingRules.Select(alertingRule => {
        var alertReceiver = alertReceiversById[alertingRule.AlertReceiverId];
        var isFiring = activeAlertsByName.TryGetValue(alertingRule.Name, out var maybeActiveAlert);

        // get silence details if alert is silenced
        AlertSilenceView? silenceDetails = null;
        // find silence that is associated with alert rule
        var existingSilence = serviceSilences
          .OrderByDescending(s => s.UpdatedAt)
          .FirstOrDefault(s =>
            s.Matchers.Any(m =>
              m.Name == "alertname" && m.Value == alertingRule.Name));

        if (existingSilence != null) {
          // active silence exists
          silenceDetails = new AlertSilenceView(
            existingSilence.StartsAt.DateTime,
            existingSilence.EndsAt.DateTime,
            existingSilence.CreatedBy);
        }

        return new ServiceAlert(
          name: alertingRule.Name,
          threshold: alertingRule.Threshold,
          receiverName: alertReceiver.Name,
          receiverType: alertReceiver.Type,
          isFiring: isFiring,
          since: maybeActiveAlert?.StartsAt.DateTime,
          isSilenced: silenceDetails != null,
          silenceDetails: silenceDetails);
      })
        .ToImmutableList());
  }

  [HttpPost("silences/{environment}/tenants/{tenant}/services/{*servicePath}",
    Name = "CreateUpdateSilence")]
  [Consumes(typeof(AlertSilenceDetails), contentType: "application/json")]
  [ProducesResponseType(statusCode: 204)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> CreateUpdateSilence(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    [FromBody] AlertSilenceDetails silenceDetails,
    CancellationToken cancellationToken = default) {

    var principal = HttpContext.User;
    if (principal == null) {
      return this.Unauthorized(new {
        Status = "Unauthorized",
        Message = "Unauthorized to access requested resource."
      });
    }

    var userEmail = principal.FindFirstValue(ClaimTypes.Email);
    // return 400 (Bad Request) if claims are missing
    if (String.IsNullOrEmpty(userEmail)) {
      return this.BadRequest(new {
        Message = "Required claims are missing."
      });
    }

    var serviceName = servicePath.Split('/').Last();
    var service = await this._serviceDataHelper.FetchExistingService(
      environment,
      tenant,
      serviceName,
      cancellationToken);

    var alertName = silenceDetails.Name;

    // duration for silence hardcoded at 1 day
    var createUpdateSilenceTask =
      this._alertmanagerService.CreateUpdateSilenceAsync(
        environment,
        tenant,
        service.Name,
        alertName,
        userEmail,
        cancellationToken);

    try {
      await createUpdateSilenceTask;
    } catch (TaskCanceledException e) {
      this._logger.LogError(exception: e, message: "Task cancelled while silencing alert via Alertmanager.");
      return this.StatusCode(StatusCodes.Status504GatewayTimeout);
    }

    return this.NoContent();
  }

  [HttpPut("silences/{environment}/tenants/{tenant}/services/{*servicePath}",
    Name = "RemoveSilence")]
  [ProducesResponseType(statusCode: 204)]
  [Consumes(typeof(AlertSilenceDetails), contentType: "application/json")]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult> RemoveSilence(
    [FromRoute] String environment,
    [FromRoute] String tenant,
    [FromRoute] String servicePath,
    [FromBody] AlertSilenceDetails silenceDetails,
    CancellationToken cancellationToken = default) {

    var principal = HttpContext.User;
    if (principal == null) {
      return this.Unauthorized(new {
        Status = "Unauthorized",
        Message = "Unauthorized to access requested resource."
      });
    }

    var serviceName = servicePath.Split('/').Last();

    var service = await this._serviceDataHelper.FetchExistingService(
      environment,
      tenant,
      serviceName,
      cancellationToken);

    var alertName = silenceDetails.Name;
    var deleteSilenceTask = this._alertmanagerService.DeleteSilenceAsync(
      environment,
      tenant,
      service.Name,
      alertName,
      cancellationToken);

    try {
      await deleteSilenceTask;
    } catch (TaskCanceledException e) {
      this._logger.LogError(exception: e, message: "Task cancelled while un-silencing alert via Alertmanager.");
      return this.StatusCode(StatusCodes.Status504GatewayTimeout);
    }

    return this.NoContent();
  }
}
