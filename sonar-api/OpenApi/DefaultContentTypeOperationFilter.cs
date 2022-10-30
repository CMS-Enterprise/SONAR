using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cms.BatCave.Sonar.OpenApi;

public class DefaultContentTypeOperationFilter : IOperationFilter {
  public void Apply(
    OpenApiOperation operation,
    OperationFilterContext context) {

    var hasConsumesAttribute = context.MethodInfo.GetCustomAttributes<ConsumesAttribute>().Any();
    if ((operation.RequestBody != null) &&
      !hasConsumesAttribute &&
      operation.RequestBody.Content.ContainsKey("application/json")) {

      // drop all keys besides "application/json"
      foreach (var key in operation.RequestBody.Content.Keys.Where(k => k != "application/json")) {
        operation.RequestBody.Content.Remove(key);
      }
    }

    var hasProducesAttribute = context.MethodInfo.GetCustomAttributes<ProducesAttribute>().Any();
    if (!hasProducesAttribute) {
      foreach (var response in operation.Responses) {
        // drop all keys besides "application/json"
        foreach (var key in response.Value.Content.Keys.Where(k => k != "application/json")) {
          response.Value.Content.Remove(key);
        }
      }
    }
  }
}
