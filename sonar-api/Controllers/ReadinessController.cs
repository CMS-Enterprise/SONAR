using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Cms.BatCave.Sonar.Controllers;

[ApiController]
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
