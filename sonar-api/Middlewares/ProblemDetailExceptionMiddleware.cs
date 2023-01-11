using System;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Cms.BatCave.Sonar.Middlewares;

public class ProblemDetailExceptionMiddleware {
  private readonly RequestDelegate _next;

  public ProblemDetailExceptionMiddleware(RequestDelegate next) {
    this._next = next;
  }

  public async Task InvokeAsync(HttpContext context) {
    try {
      await this._next(context);
    } catch (ProblemDetailException ex) {
      context.Response.StatusCode = (Int32)ex.Status;
      await context.Response.WriteAsJsonAsync(ex.ToProblemDetails(), context.RequestAborted);
    }
  }
}
