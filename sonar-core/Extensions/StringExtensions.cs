using System;

namespace Cms.BatCave.Sonar.Extensions;

public static class StringExtensions {
  public static String ToCamelCase(this String source) {
    return $"{source.Substring(0, 1).ToLowerInvariant()}{source.Substring(1)}";
  }
}
