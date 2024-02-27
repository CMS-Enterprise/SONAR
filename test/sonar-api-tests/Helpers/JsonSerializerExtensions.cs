using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;

namespace Cms.BatCave.Sonar.Tests.Helpers;

public static class JsonSerializerExtensions {
  /// <summary>
  /// Unfortunately Deserialize&lt;ExpandoObject> only shallowly deserializes JsonElement into ExpandoObject/IEnumerable.
  /// </summary>
  public static ExpandoObject? DynamicDeserialize(
    this JsonElement json,
    JsonSerializerOptions? options = null) {

    var shallow = json.Deserialize<ExpandoObject>(options);

    if (shallow != null) {
      var clone = new ExpandoObject();
      foreach (var prop in shallow) {
        if (prop.Value is JsonElement element) {
          Object? value;
          switch (element.ValueKind) {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
              value = null;
              break;
            case JsonValueKind.Object:
              value = DynamicDeserialize(element);
              break;
            case JsonValueKind.Array:
              value = DeserializeArray(element);
              break;
            case JsonValueKind.String:
              value = element.GetString();
              break;
            case JsonValueKind.Number:
              if (element.TryGetInt32(out var i32)) {
                value = i32;
              } else if (element.TryGetInt64(out var i64)) {
                value = i64;
              } else if (element.TryGetDouble(out var fp)) {
                value = fp;
              } else {
                throw new JsonException($"Unable to deserialize numeric value: {element.GetRawText()}");
              }

              break;
            case JsonValueKind.True:
            case JsonValueKind.False:
              value = element.GetBoolean();
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }

          clone.TryAdd(prop.Key, value);
        } else {
          clone.TryAdd(prop.Key, prop.Value);
        }
      }

      return clone;
    } else {
      return null;
    }
  }

  private static IEnumerable<ExpandoObject?> DeserializeArray(JsonElement array) {
    var enumerator = array.EnumerateArray();
    while (enumerator.MoveNext()) {
      yield return DynamicDeserialize(enumerator.Current);
    }
  }
}
