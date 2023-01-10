using System;
using System.Net;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public class UnauthorizedException : ProblemDetailException {
  private const String ProblemTypeName = "Unauthorized";

  public override String ProblemType => UnauthorizedException.ProblemTypeName;

  public UnauthorizedException(String message) :
    base(HttpStatusCode.Unauthorized, message) { }
}
