using System;
using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[Route("api/ready")]
public class ReadinessController : ControllerBase {
  [HttpGet]
  public ActionResult<Object> Get() {
    return this.Ok(new {
      Status = "Ok",
      Version =
        Assembly.GetEntryAssembly()
          ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
          ?.InformationalVersion
    });
  }
}
