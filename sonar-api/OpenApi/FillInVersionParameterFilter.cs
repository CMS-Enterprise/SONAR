using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cms.BatCave.Sonar.OpenApi;

public class FillInVersionParameterFilter : IDocumentFilter {
  public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context) {
    foreach (var path in swaggerDoc.Paths.Keys.ToList()) {
      var pathDetail = swaggerDoc.Paths[path];
      var replaceVersion = false;
      foreach (var operation in pathDetail.Operations.Values) {
        for (var ix = 0; ix < operation.Parameters.Count; ix++) {
          var param = operation.Parameters[ix];
          if (param.Name == "version") {
            operation.Parameters.RemoveAt(ix);
            replaceVersion = true;
            break;
          }
        }
      }

      if (replaceVersion) {
        swaggerDoc.Paths.Remove(path);
        swaggerDoc.Paths.Add(path.Replace("v{version}", context.DocumentName), pathDetail);
      }
    }
  }
}
