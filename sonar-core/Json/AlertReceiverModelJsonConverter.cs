using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Extensions;

namespace Cms.BatCave.Sonar.Json;

public class AlertReceiverModelJsonConverter : JsonConverter<AlertReceiverConfiguration> {

  public override AlertReceiverConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
    var document = JsonDocument.ParseValue(ref reader);
    var element = document.RootElement;

    switch (element.ValueKind) {
      case JsonValueKind.Undefined:
      case JsonValueKind.Null:
        return null;
      case JsonValueKind.Object:
        // Get the type
        var hasType = TryParseReceiversType(element, out var type);

        String? name = null;
        AlertReceiverOptions? receiverOptions = null;

        if (hasType &&
          element.TryGetProperty(nameof(AlertReceiverConfiguration.Options), ignoreCase: true, out var optionElement)) {

          switch (type) {
            case AlertReceiverType.Email:
              receiverOptions = optionElement.Deserialize<AlertReceiverOptionsEmail>(options);
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }

        if (element.TryGetProperty(nameof(AlertReceiverConfiguration.Name), ignoreCase: true, out var nameElement)) {
          if (!nameElement.IsStringOrNull()) {
            throw new JsonException(
              $"Unexpected JSON value {nameElement.ValueKind}. Was expecting a string for the {nameof(AlertReceiverConfiguration)}.{nameof(AlertReceiverConfiguration.Name)} property."
            );
          }
          name = nameElement.GetString();
        }

        // Intentionally disregard nullability constraints in the same way that JsonSerializer does.
        return (AlertReceiverConfiguration)Activator.CreateInstance(
          typeof(AlertReceiverConfiguration),
          name,
          type,
          receiverOptions
        )!;

      case JsonValueKind.Array:
      case JsonValueKind.String:
      case JsonValueKind.Number:
      case JsonValueKind.True:
      case JsonValueKind.False:
      default:
        throw new JsonException(
          $"Expected object attempting tp parse {nameof(AlertReceiverConfiguration)} but found {element.ValueKind}"
        );
    }
  }

  private static Boolean TryParseReceiversType(JsonElement element, out AlertReceiverType type) {
    if (!element.TryGetProperty(nameof(AlertReceiverConfiguration.Type), ignoreCase: true, out var typeValue) ||
      typeValue.IsNullOrUndefined()) {
      type = default;
      return false;
    }

    if (typeValue.ValueKind != JsonValueKind.String) {
      throw new JsonException(
        $"Unexpected JSON value {typeValue.ValueKind}. Was expecting a string for the {nameof(AlertReceiverConfiguration)}.{nameof(AlertReceiverConfiguration.Type)} property."
      );
    }

    if (!Enum.TryParse(typeValue.GetString(), ignoreCase: true, out type)) {
      throw new JsonException(
        $"Invalid value for property {nameof(AlertReceiverConfiguration)}.{nameof(AlertReceiverConfiguration.Type)}: {typeValue.GetString()}."
      );
    }

    return true;
  }
  public override void Write(Utf8JsonWriter writer, AlertReceiverConfiguration value, JsonSerializerOptions options) {
    writer.WriteStartObject();
    writer.WriteString(nameof(AlertReceiverConfiguration.Name).ToCamelCase(), value.Name);
    writer.WriteString(nameof(AlertReceiverConfiguration.Type).ToCamelCase(), value.Type.ToString());
    writer.WritePropertyName("options");

    switch (value.Type) {
      case AlertReceiverType.Email:
        if (value.Options is AlertReceiverOptionsEmail oe) {
          JsonSerializer.Serialize(writer, oe, options);
        }
        break;
      default:
        throw new NotSupportedException(
          $"Unable to deserialize definition. Unsupported receiver type: {value.Type}");
    }
    writer.WriteEndObject();
  }
}
