using Microsoft.AspNetCore.Mvc;

namespace http_metric_test_app.Controllers;

/// <summary>
/// An API controller that returns alternating 200/400 HTTP status responses at a fixed rate.
/// Period of time to spend in each state is configurable via query parameter, default is to
/// alternate every 30s.
/// </summary>
[ApiController]
[Route("/api/succeedfail")]
public class AlternatingSucceedFailController : ControllerBase {

  [HttpGet]
  public ActionResult Get([FromQuery] TimeSpan? period) {
    var periodSeconds = (Int64)(period ?? TimeSpan.FromSeconds(30)).TotalSeconds;
    var secondsSinceEpoch = (Int64)(DateTime.Now - DateTimeOffset.UnixEpoch).TotalSeconds;
    return ((secondsSinceEpoch / periodSeconds) % 2) == 0
      ? this.Ok(new { StatusCode = StatusCodes.Status200OK, Status = "OK" })
      : this.BadRequest(new { StatusCode = StatusCodes.Status400BadRequest, Status = "Bad Request" });
  }

}
