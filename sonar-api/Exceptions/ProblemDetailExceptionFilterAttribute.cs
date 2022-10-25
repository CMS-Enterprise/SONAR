using System;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cms.BatCave.Sonar.Exceptions;

public class ProblemDetailExceptionFilterAttribute : ExceptionFilterAttribute {
  public override void OnException(ExceptionContext context) {
    if (context.Exception is ProblemDetailException ex) {
      context.HttpContext.Response.StatusCode = (Int32)ex.Status;
      context.Result = new ObjectResult(ex.ToProblemDetails());
    } else {
      base.OnException(context);
    }
  }
}
