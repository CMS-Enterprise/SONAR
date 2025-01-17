using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Logger;

public sealed class CustomFormatter : ConsoleFormatter, IDisposable {

  private static readonly ImmutableHashSet<String> ReservedKeys = ImmutableHashSet.Create<String>(
    StringComparer.OrdinalIgnoreCase,
    "level",
    "category",
    "eventId",
    "message",
    "exception",
    "{OriginalFormat}"
  );

  private readonly IDisposable? _optionsReloadToken;
  private LoggingCustomOptions _formatterOptions;
  private readonly String _defaultConsoleColor;

  public CustomFormatter(IOptionsMonitor<LoggingCustomOptions> options) :
    base(nameof(CustomFormatter)) {
    this._optionsReloadToken = options.OnChange(this.ReloadLoggerOptions);
    this._formatterOptions = options.CurrentValue;
    this._defaultConsoleColor = "\x1b[0m";
  }

  private void ReloadLoggerOptions(LoggingCustomOptions options) => this._formatterOptions = options;

  public override void Write<TState>(
    in LogEntry<TState> logEntry,
    IExternalScopeProvider? scopeProvider,
    TextWriter textWriter) {

    // Wrap level in coloring (<= Debug -> Grey, == Info -> Green, == Warning -> Yellow, >= Error -> Red) if enabled
    // Note: LogLevel is a numeric enum where more severe levels have greater numeric value
    var sb = new StringBuilder();
    if (this._formatterOptions.EnableColor) {
      var logLevelColor = CustomFormatter.GetLogLevelColor(logEntry.LogLevel);
      sb.Append($"level={logLevelColor}{logEntry.LogLevel}{this._defaultConsoleColor} category={logEntry.Category}");
    } else {
      sb.Append($"level={logEntry.LogLevel} category={logEntry.Category}");
    }

    if (logEntry.EventId != default) {
      sb.Append($" eventId={logEntry.EventId}");
    }

    if (logEntry.Formatter != null) {
      sb.Append($" message=\"{logEntry.Formatter(logEntry.State, logEntry.Exception).Escape()}\"");

      if (logEntry.State is IEnumerable<KeyValuePair<String, Object?>> data) {
        foreach (var kvp in data) {
          if (!ReservedKeys.Contains(kvp.Key) && !kvp.Key.StartsWith("_") && (kvp.Value != null)) {
            sb.Append($" {kvp.Key.Replace(' ', '_')}=\"{kvp.Value.ToString()?.Escape()}\"");
          }
        }
      }
    }

    if (logEntry.Exception != null) {
      sb.Append($" exception=\"{logEntry.Exception.ToString().Escape()}\"");
    }

    textWriter.WriteLine(sb.ToString());
  }

  private static String GetLogLevelColor(LogLevel level) {

    var levelColor = "";

    switch (level) {
      case LogLevel.Debug:
        levelColor = "\x1b[90m"; // grey
        break;
      case LogLevel.Information:
        levelColor = "\x1b[32m"; // green
        break;
      case LogLevel.Warning:
        levelColor = "\x1b[33m"; // yellow
        break;
      case LogLevel.Error:
        levelColor = "\x1b[31m"; // red
        break;
    }

    return levelColor;
  }

  public void Dispose() => this._optionsReloadToken?.Dispose();
}
