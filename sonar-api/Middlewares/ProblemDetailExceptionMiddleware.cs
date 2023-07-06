using System;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Cms.BatCave.Sonar.Middlewares;

public class ProblemDetailExceptionMiddleware : Middleware {
  public ProblemDetailExceptionMiddleware(RequestDelegate next) : base(next) {
  }

  public override async Task InvokeAsync(HttpContext context) {
    try {
      await this.Next(context);
    } catch (ProblemDetailException ex) {
      context.Response.StatusCode = (Int32)ex.Status;
      await context.Response.WriteAsJsonAsync(ex.ToProblemDetails(), context.RequestAborted);
    }
  }
}
