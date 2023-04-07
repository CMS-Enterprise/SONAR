using System;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/services")]
public class LegacyController : ControllerBase {

  [HttpGet]
  public IActionResult GetAllServices() {
    throw new NotImplementedException("TODO");
  }
}
