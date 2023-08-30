using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace http_metric_test_app.Controllers;

[ApiController]
[Route("api/ready")]
public class ApiReadinessController : ControllerBase {
  private static readonly Int16[] Statuses = { 200, 400 };

  [HttpGet]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 404)]
  public async Task<ActionResult<Object>> Get() {
    // Randomize status and return.
    Random r = new Random();
    var statusValue = Statuses[r.Next(2)];
    var version = Assembly.GetEntryAssembly()
      ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
      ?.InformationalVersion;

    // Simulate random request timeout
    if (r.Next(101) > 70) {
      Console.WriteLine("Delay...");
      await Task.Delay(TimeSpan.FromSeconds(2));
    }

    switch (statusValue) {
      case 200:
        await Task.Delay(TimeSpan.FromSeconds(2));
        Console.WriteLine("200");
        return this.Ok(new {
          Status = "Ok",
          Version = version
        });
      case 400:
        Console.WriteLine("400");
        return this.BadRequest(new ProblemDetails {
          Title = "Example 400 error.",
          Type = "test 400"
        });
      default:
        return this.NotFound(new ProblemDetails {
          Title = "Example 404 error.",
          Type = "test 404"
        });
    }
  }
}
