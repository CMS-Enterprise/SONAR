using System;

namespace Cms.BatCave.Sonar.Extensions;

public static class StringExtensions {
  public static String ToCamelCase(this String source) {
    return $"{source.Substring(0, 1).ToLowerInvariant()}{source.Substring(1)}";
  }

  /// <summary>
  /// Escapes any Backslash, Newline, and Quote characters in a string by preceding them with a backslash.
  /// </summary>
  /// <param name="str">The string to escape</param>
  /// <param name="quote">The type of quote character to escape (default: ")</param>
  /// <returns>An escaped version of the original string.</returns>
  /// <remarks>This implementation is designed for simplicity not performance.</remarks>
  public static String Escape(this String str, Char quote = '"') =>
    str.Replace(oldValue: "\\", newValue: "\\\\").ReplaceLineEndings("\\n").Replace(oldValue: quote.ToString(), newValue: $"\\{quote}");
}
