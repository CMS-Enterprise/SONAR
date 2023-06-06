using System;
using Microsoft.Extensions.Logging;
//using Cms.BatCave.Sonar.Agent.Logger;

//namespace Cms.BatCave.Sonar.Agent.Logger;
namespace Cms.BatCave.Sonar.Logger;

public static class ConsoleLoggerExtensions {
  public static ILoggingBuilder AddCustomFormatter(
    this ILoggingBuilder builder,
    Action<LoggingCustomOptions> configure) =>
    builder.AddConsole(options => options.FormatterName = nameof(CustomFormatter))
      .AddConsoleFormatter<CustomFormatter, LoggingCustomOptions>(configure);
}
