using System.Reflection;
using System.Net;
using System.Xml;
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

  [HttpGet("statusxml", Name = "GetXmlStatus")]
  [ProducesResponseType(typeof(String), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [ProducesResponseType(500)]
  public Task<ContentResult> GetXMLStatus(CancellationToken cancellationToken = default) {

    String resultString = """
              <?xml version="1.0" encoding="UTF-8"?>
               <GetSystemCheckResponse>
                <status>success</status>
                <code>100</code>
                <messages/>
                <subsystem1Status>Yes</subsystem1Status>
                <subsystem2Status>Yes</subsystem2Status>
                <subsystem3Status>Yes</subsystem3Status>
               </GetSystemCheckResponse>
              """;

    return Task.FromResult<ContentResult>(
          new ContentResult {
            Content = resultString,
            ContentType = "application/xml",
            StatusCode = (Int32)HttpStatusCode.OK
          }
      );
  }

  [HttpGet("statusjson", Name = "GetJsonStatus")]
  [ProducesResponseType(typeof(String), statusCode: 200)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 400)]
  [ProducesResponseType(typeof(ProblemDetails), statusCode: 401)]
  [ProducesResponseType(500)]
  public Task<ContentResult> GetJsonStatus(CancellationToken cancellationToken = default) {

    String resultString = """
          {
          "responseHeader":
          {
          "zkConnected":null,
          "status":0,
          "QTime":0,
          "params":
          {
          "q":"{!lucene}*:*",
          "distrib":"false",
          "df":"text",
          "rows":"10",
          "echoParams":"all",
          "rid":"-195846"
          }
          },
          "status":"OK"
          }
          """;

    return Task.FromResult<ContentResult>(
      new ContentResult {
        Content = resultString,
        ContentType = "application/json",
        StatusCode = (Int32)HttpStatusCode.OK
      });
  }
}
