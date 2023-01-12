using System;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Tests;

public class LogMessageEventArgs : EventArgs {
  public LogLevel Level { get; }
  public String Message { get; }

  public LogMessageEventArgs(LogLevel level, String message) {
    this.Level = level;
    this.Message = message;
  }
}
