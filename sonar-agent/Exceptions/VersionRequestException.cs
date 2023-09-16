using System;

namespace Cms.BatCave.Sonar.Agent.Exceptions;

public class VersionRequestException : Exception {
  public VersionRequestException() { }

  public VersionRequestException(String? message)
    : base(message) { }

  public VersionRequestException(String? message, Exception? innerException)
    : base(message, innerException) { }
}
