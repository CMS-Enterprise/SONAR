using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Cms.BatCave.Sonar.Middlewares;


public abstract class MiddlewareBase {
  protected RequestDelegate Next { get; }

  protected MiddlewareBase(RequestDelegate next) {
    this.Next = next;
  }
}

/// <summary>
/// An abstract base class for ASP.Net Core Middleware.
/// </summary>
public abstract class Middleware : MiddlewareBase {
  protected Middleware(RequestDelegate next) : base(next) {
  }

  /// <summary>
  /// When implemented in a derived class, performs the middleware functionality and calls the Next request delegate.
  /// </summary>
  public abstract Task InvokeAsync(HttpContext context);
}

public abstract class Middleware<T1> : MiddlewareBase {
  protected Middleware(RequestDelegate next) : base(next) {
  }

  /// <summary>
  /// When implemented in a derived class, performs the middleware functionality. Implementations of this method must call the Next request delegate.
  /// </summary>
  public abstract Task InvokeAsync(HttpContext context, T1 dep1);
}

public abstract class Middleware<T1, T2> : MiddlewareBase {
  protected Middleware(RequestDelegate next) : base(next) {
  }

  /// <summary>
  /// When implemented in a derived class, performs the middleware functionality. Implementations of this method must call the Next request delegate.
  /// </summary>
  public abstract Task InvokeAsync(HttpContext context, T1 dep1, T2 dep2);
}
