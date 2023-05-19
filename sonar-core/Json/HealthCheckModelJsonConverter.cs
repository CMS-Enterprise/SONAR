using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Json;

/// <summary>
///   Handles the serialization and deserialization of <see cref="HealthCheckModel" />. Special
///   handling is required because the <see cref="HealthCheckModel.Definition" /> property has the
///   abstract type <see cref="HealthCheckDefinition" /> so the <see cref="HealthCheckModel.Type" />
///   property must be used to determine the appropriate type to deserialize.
/// </summary>
public class HealthCheckModelJsonConverter : JsonConverter<HealthCheckModel> {
  /// <summary>
  ///   Deserialize a <see cref="HealthCheckModel" /> by reading a <see cref="JsonElement" /> from the
  ///   specified <paramref name="reader" /> and deserializing the
  ///   <see cref="HealthCheckModel.Definition" /> based on the specified
  ///   <see cref="HealthCheckModel.Type" />.
  /// </summary>
  public override HealthCheckModel? Read(
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
        var hasType = TryParseHealthCheckType(element, out var type);

        String? name = null;
        String? description = null;
        HealthCheckDefinition? definition = null;

        if (hasType &&
          element.TryGetProperty(nameof(HealthCheckModel.Definition), ignoreCase: true, out var definitionElement)) {

          switch (type) {
            case HealthCheckType.PrometheusMetric:
              definition = definitionElement.Deserialize<MetricHealthCheckDefinition>(options);

              break;
            case HealthCheckType.LokiMetric:
              definition = definitionElement.Deserialize<MetricHealthCheckDefinition>(options);

              break;
            case HealthCheckType.HttpRequest:
              definition = definitionElement.Deserialize<HttpHealthCheckDefinition>(options);

              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }

        if (element.TryGetProperty(nameof(HealthCheckModel.Name), ignoreCase: true, out var nameElement)) {
          if (!nameElement.IsStringOrNull()) {
            throw new JsonException(
              $"Unexpected JSON value {nameElement.ValueKind}. Was expecting a string for the {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Name)} property."
            );
          }

          name = nameElement.GetString();
        }

        if (element.TryGetProperty(nameof(HealthCheckModel.Description), ignoreCase: true,
          out var descriptionElement)) {
          if (!descriptionElement.IsStringOrNull()) {
            throw new JsonException(
              $"Unexpected JSON value {descriptionElement.ValueKind}. Was expecting a string for the {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Description)} property."
            );
          }

          description = descriptionElement.GetString();
        }

        // Intentionally disregard nullability constraints in the same way that JsonSerializer does.
        return (HealthCheckModel)Activator.CreateInstance(
          typeof(HealthCheckModel),
          name,
          description,
          type,
          definition
        )!;
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
  ///   Attempts to parse the type property of a JsonElement representing a <see cref="HealthCheckModel" />.
  /// </summary>
  private static Boolean TryParseHealthCheckType(JsonElement element, out HealthCheckType type) {
    if (!element.TryGetProperty(nameof(HealthCheckModel.Type), ignoreCase: true, out var typeValue) ||
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
  ///   Serialize the specified <see cref="HealthCheckModel" /> to the provided <paramref name="writer" />.
  /// </summary>
  public override void Write(
    Utf8JsonWriter writer,
    HealthCheckModel value,
    JsonSerializerOptions options) {

    writer.WriteStartObject();
    writer.WriteString(nameof(HealthCheckModel.Name).ToCamelCase(), value.Name);
    writer.WriteString(nameof(HealthCheckModel.Description).ToCamelCase(), value.Description);
    writer.WriteString(nameof(HealthCheckModel.Type).ToCamelCase(), value.Type.ToString());

    writer.WritePropertyName(nameof(HealthCheckModel.Definition).ToCamelCase());
    switch (value.Type) {
      case HealthCheckType.PrometheusMetric:
      case HealthCheckType.LokiMetric:
        JsonSerializer.Serialize(writer, (MetricHealthCheckDefinition)value.Definition, options);
        break;
      case HealthCheckType.HttpRequest:
        JsonSerializer.Serialize(writer, (HttpHealthCheckDefinition)value.Definition, options);
        break;
      default:
        throw new NotSupportedException(
          $"Unable to deserialize definition. Unsupported health check type: {value.Type}");
    }

    writer.WriteEndObject();
  }
}
