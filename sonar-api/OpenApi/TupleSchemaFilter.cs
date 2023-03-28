using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cms.BatCave.Sonar.OpenApi;

public class TupleSchemaFilter : ISchemaFilter {
  public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
    if (typeof(ITuple).IsAssignableFrom(context.Type)) {
      var ctor = context.Type.GetConstructors().Single();
      var parameters = ctor.GetParameters();
      schema.Type = "array";
      schema.MinItems = parameters.Length;
      schema.MaxItems = parameters.Length;
      schema.Items = new OpenApiSchema {
        Title = "Item",
        OneOf = parameters
          .Select(p => context.SchemaGenerator.GenerateSchema(p.ParameterType, context.SchemaRepository))
          .ToList()
      };
    }
  }
}
