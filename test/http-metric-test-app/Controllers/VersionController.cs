using Microsoft.AspNetCore.Mvc;

namespace http_metric_test_app.Controllers;

[ApiController]
[Route("api/version")]
public class VersionController : ControllerBase {

  [HttpGet]
  [ProducesResponseType(typeof(ApiVersionInfo), statusCode: 200)]
  public ActionResult<Object> Get() {
    return this.Ok(new ApiVersionInfo("v0.0.0-test"));
  }

  public record ApiVersionInfo(String Version) {
  }
}
