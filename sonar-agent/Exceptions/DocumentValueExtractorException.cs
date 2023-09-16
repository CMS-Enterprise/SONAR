using System;

namespace Cms.BatCave.Sonar.Agent.Exceptions;

public class DocumentValueExtractorException : Exception {
  public DocumentValueExtractorException() { }

  public DocumentValueExtractorException(String? message)
    : base(message) { }

  public DocumentValueExtractorException(String? message, Exception? innerException)
    : base(message, innerException) { }
}
