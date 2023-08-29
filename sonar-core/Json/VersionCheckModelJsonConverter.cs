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
        // Get the type
        var hasType = TryParseVersionCheckType(element, out var type);

        VersionCheckDefinition? definition = null;
        if (hasType &&
          element.TryGetProperty(nameof(VersionCheckModel.Definition), ignoreCase: true, out var definitionElement)) {

          switch (type) {
            case VersionCheckType.FluxKustomization:
              definition = definitionElement.Deserialize<FluxKustomizationVersionCheckDefinition>(options);

              break;
            // TODO: Add case for HTTP version check here
            default:
              throw new ArgumentOutOfRangeException();
          }
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
          $"Expected object attempting tp parse {nameof(HealthCheckModel)} but found {element.ValueKind}"
        );
    }
  }

  /// <summary>
  ///   Attempts to parse the type property of a JsonElement representing a <see cref="VersionCheckModel" />.
  /// </summary>
  private static Boolean TryParseVersionCheckType(JsonElement element, out VersionCheckType type) {
    if (!element.TryGetProperty(nameof(VersionCheckModel.VersionCheckType), ignoreCase: true, out var typeValue) ||
      typeValue.IsNullOrUndefined()) {

      type = default;
      return false;
    }

    if (typeValue.ValueKind != JsonValueKind.String) {
      throw new JsonException(
        $"Unexpected JSON value {typeValue.ValueKind}. Was expecting a string for the {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Type)} property."
      );
    }

    if (!Enum.TryParse(typeValue.GetString(), ignoreCase: true, out type)) {
      throw new JsonException(
        $"Invalid value for property {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Type)}: {typeValue.GetString()}."
      );
    }

    return true;
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
      default:
        throw new NotSupportedException(
          $"Unable to deserialize definition. Unsupported health check type: {value.VersionCheckType}");
    }

    writer.WriteEndObject();
  }
}
