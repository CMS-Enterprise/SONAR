using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Json;

public class VersionCheckModelJsonConverter : JsonConverter<VersionCheckModel> {
  public override VersionCheckModel? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options) {

    var document = JsonDocument.ParseValue(ref reader);
    var element = document.RootElement;

    switch (element.ValueKind) {
      case JsonValueKind.Undefined:
      case JsonValueKind.Null:
        return null;
      case JsonValueKind.Object:
        var type = ParseVersionCheckType(element);
        VersionCheckDefinition? definition = null;

        if (element.TryGetProperty(nameof(VersionCheckModel.Definition), ignoreCase: true, out var definitionElement)) {
          definition = type switch {
            VersionCheckType.FluxKustomization =>
              definitionElement.Deserialize<FluxKustomizationVersionCheckDefinition>(options),
            VersionCheckType.HttpResponseBody =>
              definitionElement.Deserialize<HttpResponseBodyVersionCheckDefinition>(options),
            _ =>
              throw new NotSupportedException($"Unable to deserialize {nameof(VersionCheckModel)}, " +
                $"unsupported {nameof(VersionCheckType)}: {type}")
          };
        }

        return (VersionCheckModel)Activator.CreateInstance(
          typeof(VersionCheckModel),
          type,
          definition)!;

      case JsonValueKind.Array:
      case JsonValueKind.String:
      case JsonValueKind.Number:
      case JsonValueKind.True:
      case JsonValueKind.False:
      default:
        throw new JsonException(
          $"Expected object attempting to parse {nameof(VersionCheckModel)} but found {element.ValueKind}"
        );
    }
  }

  private static VersionCheckType ParseVersionCheckType(JsonElement modelElement) {
    const String propertyName = nameof(VersionCheckModel.VersionCheckType);

    if (!modelElement.TryGetProperty(propertyName, ignoreCase: true, out var propertyElement) ||
      propertyElement.IsNullOrUndefined()) {
      throw new JsonException($"The {propertyName} field is required.");
    }

    if (propertyElement.ValueKind != JsonValueKind.String) {
      throw new JsonException($"Expected string attempting to parse {propertyName} " +
        $"but found {propertyElement.ValueKind}");
    }

    if (Enum.TryParse(propertyElement.GetString(), ignoreCase: true, out VersionCheckType enumValue)) {
      return enumValue;
    }

    throw new JsonException($"Invalid value for {propertyName}: {propertyElement.GetString()}.");

  }

  /// <summary>
  ///   Serialize the specified <see cref="VersionCheckModel" /> to the provided <paramref name="writer" />.
  /// </summary>
  public override void Write(
    Utf8JsonWriter writer,
    VersionCheckModel value,
    JsonSerializerOptions options) {

    writer.WriteStartObject();
    writer.WriteString(nameof(VersionCheckModel.VersionCheckType).ToCamelCase(), value.VersionCheckType.ToString());

    writer.WritePropertyName(nameof(VersionCheckModel.Definition).ToCamelCase());
    switch (value.VersionCheckType) {
      case VersionCheckType.FluxKustomization:
        JsonSerializer.Serialize(writer, (FluxKustomizationVersionCheckDefinition)value.Definition, options);
        break;
      case VersionCheckType.HttpResponseBody:
        JsonSerializer.Serialize(writer, (HttpResponseBodyVersionCheckDefinition)value.Definition, options);
        break;
      default:
        throw new NotSupportedException($"Unable to serialize {nameof(VersionCheckModel)}, " +
          $"unsupported {nameof(VersionCheckType)}: {value.VersionCheckType}");
    }

    writer.WriteEndObject();
  }
}
