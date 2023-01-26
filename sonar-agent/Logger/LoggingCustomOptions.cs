using System;
using Microsoft.Extensions.Logging.Console;

namespace Cms.BatCave.Sonar.Agent.Logger;

public sealed class LoggingCustomOptions : ConsoleFormatterOptions {
  public Boolean EnableColor { get; set; }
}
