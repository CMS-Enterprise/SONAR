using System;
using System.Net;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public class InternalServerErrorException : ProblemDetailException {
  private const String DefaultErrorType = "UnknownError";

  public InternalServerErrorException(String errorType, String message) :
    base(HttpStatusCode.InternalServerError, message) {

    this.ProblemType = errorType;
  }

  public InternalServerErrorException(String message) :
    this(errorType: InternalServerErrorException.DefaultErrorType, message) {
  }

  [Obsolete(DiagnosticId = "SYSLIB0051")]
  public InternalServerErrorException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this.ProblemType = info.GetString(nameof(this.ProblemType)) ?? InternalServerErrorException.DefaultErrorType;
  }

  public override String ProblemType { get; }
}
