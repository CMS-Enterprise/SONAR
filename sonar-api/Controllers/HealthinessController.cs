using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[Route("api/healthy")]
public class HealthinessController : ControllerBase {

  private readonly HealthDataHelper _healthDataHelper;

  public HealthinessController(HealthDataHelper healthDataHelper) {
    this._healthDataHelper = healthDataHelper;
  }

  [HttpGet]
  [ProducesResponseType(typeof(String), (Int32)HttpStatusCode.OK, MediaTypeNames.Text.Plain)]
  [ProducesResponseType(typeof(String), (Int32)HttpStatusCode.ServiceUnavailable, MediaTypeNames.Text.Plain)]
  public async Task<ActionResult<String>> Get(CancellationToken cancellationToken) {
    var serviceHealths = await this._healthDataHelper.CheckSonarHealth(cancellationToken);

    var sonarAggregateStatus = HealthStatus.Online;
    var servicesByStatus = new Dictionary<HealthStatus, ISet<String>>();

    foreach (var serviceHealth in serviceHealths) {
      var serviceStatus = serviceHealth.AggregateStatus ?? HealthStatus.Unknown;

      if (serviceStatus.IsWorseThan(sonarAggregateStatus)) {
        sonarAggregateStatus = serviceStatus;
      }

      var servicesByCurrentStatus = servicesByStatus.GetValueOrDefault(serviceStatus, new HashSet<String>());
      servicesByCurrentStatus.Add(serviceHealth.DisplayName);
      servicesByStatus[serviceStatus] = servicesByCurrentStatus;
    }

    Int32 responseStatusCode;
    String responseBody;

    if (sonarAggregateStatus.IsWorseThan(HealthStatus.AtRisk)) {
      responseStatusCode = (Int32)HttpStatusCode.ServiceUnavailable;
      responseBody = $"{sonarAggregateStatus}";
      foreach (var (status, services) in servicesByStatus) {
        responseBody += $"\nThe following components are {status}: " + String.Join(separator: ", ", services);
      }
    } else {
      responseStatusCode = (Int32)HttpStatusCode.OK;
      responseBody = sonarAggregateStatus.ToString();
    }

    return this.StatusCode(responseStatusCode, responseBody);
  }

}
