using System;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cms.BatCave.Sonar.Exceptions;

public class ProblemDetailExceptionFilterAttribute : ExceptionFilterAttribute {
  public override void OnException(ExceptionContext context) {
    if (context.Exception is ProblemDetailException ex) {
      context.HttpContext.Response.StatusCode = (Int32)ex.Status;
      var detail = new ProblemDetails {
        Status = (Int32)ex.Status,
        Title = ex.Message,
        Type = ex.ErrorType
      };
      foreach (var kvp in ex.GetExtensions()) {
        detail.Extensions.Add(kvp.Key.ToCamelCase(), kvp.Value);
      }

      context.Result = new ObjectResult(detail);
    } else {
      base.OnException(context);
    }
  }
}
