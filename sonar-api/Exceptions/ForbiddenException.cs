using System;
using System.Net;

namespace Cms.BatCave.Sonar.Exceptions;

public sealed class ForbiddenException : ProblemDetailException {
  private const String ProblemTypeName = "Forbidden";

  public override String ProblemType => ForbiddenException.ProblemTypeName;

  public ForbiddenException(String message) :
    base(HttpStatusCode.Forbidden, message) { }
}
