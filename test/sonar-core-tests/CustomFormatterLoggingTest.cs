using System;
using Cms.BatCave.Sonar.Logger;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class CustomFormatterLoggingTest {
  [Fact]
  public void ExceptionMessageWithCurlyBraces_LoggedSuccessfully() {
    // Configure logging
    using var loggerFactory = LoggerFactory.Create(loggingBuilder => {
      loggingBuilder
        .AddConsole(options => options.FormatterName = nameof(CustomFormatter))
        .AddConsoleFormatter<CustomFormatter, LoggingCustomOptions>();
    });

    var logger = loggerFactory.CreateLogger<CustomFormatterLoggingTest>();

    logger.LogError(
      new Exception("Invalid { format {{ string"),
      "Explicit Message"
    );
  }
}
