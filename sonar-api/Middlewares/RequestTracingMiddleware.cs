using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Middlewares;

public class RequestTracingMiddleware {
  private readonly RequestDelegate _next;

  public RequestTracingMiddleware(RequestDelegate next) {
    this._next = next;
  }

  public async Task InvokeAsync(HttpContext context, ILogger<RequestTracingMiddleware> logger) {
    logger.LogDebug(
      "Processing Request {Method} {Url} (trace-id: {TraceId})",
      context.Request.Method,
      context.Request.Path,
      context.TraceIdentifier
    );
    var start = DateTime.UtcNow;
    try {
      await this._next(context);
      logger.LogDebug(
        "Request Complete With Status {StatusCode} (duration: {Duration:F1}s, trace-id: {TraceId})",
        context.Response.StatusCode,
        DateTime.UtcNow.Subtract(start).TotalSeconds,
        context.TraceIdentifier
      );
    } catch (Exception ex) {
      logger.LogDebug(
        "Request Failed With Exception Type {ExceptionType} (duration: {Duration:F1}s, trace-id: {TraceId})",
        ex.GetType().Name,
        DateTime.UtcNow.Subtract(start).TotalSeconds,
        context.TraceIdentifier
      );

      throw;
    }
  }
}
