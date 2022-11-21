using System;
using System.Linq;
using System.Text.Json;

namespace Cms.BatCave.Sonar.Extensions;

public static class JsonElementExtensions {
  /// <summary>
  ///   Attempts to get a property from a <see cref="JsonElement" /> that has a
  ///   <see cref="JsonElement.ValueKind" /> of <see cref="JsonValueKind.Object" />.
  /// </summary>
  /// <param name="ignoreCase">
  ///   When set to <c>true</c>, this method will match property names without regard for casing.
  /// </param>
  /// <exception cref="JsonException">
  ///   There are multiple JSON properties that match the specified <paramref name="name" />.
  /// </exception>
  public static Boolean TryGetProperty(
    this JsonElement element,
    String name,
    Boolean ignoreCase,
    out JsonElement propertyElement) {

    if (ignoreCase) {
      var matches =
        element.EnumerateObject()
          .Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
          .Select(p => p.Value)
          .ToList();

      switch (matches.Count) {
        case > 1:
          throw new JsonException(
            $"Ambiguous property names. Object contained multiple properties matching the name: {name}"
          );
        case 1:
          propertyElement = matches.First();
          return true;
        default:
          propertyElement = default;
          return false;
      }
    } else {
      return element.TryGetProperty(name, out propertyElement);
    }
  }

  /// <summary>
  ///   Returns <c>true</c> if the specified <see cref="JsonElement" /> is a string value, or is null or
  ///   undefined.
  /// </summary>
  public static Boolean IsStringOrNull(this JsonElement element) {
    switch (element.ValueKind) {
      case JsonValueKind.Undefined:
      case JsonValueKind.String:
      case JsonValueKind.Null:
        return true;
      default:
        return false;
    }
  }

  /// <summary>
  ///   Returns <c>true</c> if the specified <see cref="JsonElement" /> is null or undefined.
  /// </summary>
  public static Boolean IsNullOrUndefined(this JsonElement element) {
    switch (element.ValueKind) {
      case JsonValueKind.Undefined:
      case JsonValueKind.Null:
        return true;
      default:
        return false;
    }
  }
}
