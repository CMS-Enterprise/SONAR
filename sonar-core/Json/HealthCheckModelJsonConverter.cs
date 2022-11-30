using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        if (!element.TryGetProperty(nameof(HealthCheckModel.Type), ignoreCase: true, out var typeValue) ||
          typeValue.IsNullOrUndefined()) {

          throw new JsonException(
            $"The {nameof(HealthCheckModel.Type)} property is required."
          );
        }
        if (typeValue.ValueKind != JsonValueKind.String) {
          throw new JsonException(
            $"Unexpected JSON value {typeValue.ValueKind}. Was expecting a string for the {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Type)} property."
          );
        }
        if (!Enum.TryParse<HealthCheckType>(typeValue.GetString(), ignoreCase: true, out var type)) {
          throw new JsonException(
            $"Invalid value for property {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Type)}: {typeValue.GetString()}."
          );
        }

        HealthCheckDefinition definition;
        switch (type) {
          case HealthCheckType.PrometheusMetric:
            if (!element.TryGetProperty(nameof(HealthCheckModel.Definition), ignoreCase: true, out var definitionElement)) {
              throw new JsonException($"The {nameof(HealthCheckModel.Definition)} property is required.");
            }

            definition = definitionElement.Deserialize<PrometheusHealthCheckDefinition>(options) ??
              throw new JsonException($"The {nameof(HealthCheckModel.Definition)} property is required.");

            var context = new ValidationContext(definition);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(definition, context, results, validateAllProperties: true)) {
              throw new JsonException(
                $"Invalid health check definition: {results.First().ErrorMessage}"
              );
            }
            break;
          case HealthCheckType.HttpRequest:
            if (!element.TryGetProperty(nameof(HealthCheckModel.Definition), ignoreCase: true, out var httpDefinitionElement)) {
              throw new JsonException($"The {nameof(HealthCheckModel.Definition)} property is required.");
            }

            definition = httpDefinitionElement.Deserialize<HttpHealthCheckDefinition>(options) ??
                         throw new JsonException($"The {nameof(HealthCheckModel.Definition)} property is required.");
            var httpContext = new ValidationContext(definition);
            var httpResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(definition, httpContext, httpResults, validateAllProperties: true)) {
              throw new JsonException(
                $"Invalid health check definition: {httpResults.First().ErrorMessage}"
              );
            }
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }

        if (!element.TryGetProperty(nameof(HealthCheckModel.Name), ignoreCase: true, out var nameElement)) {
          throw new JsonException($"The {nameof(HealthCheckModel.Name)} property is required.");
        }
        if (!nameElement.IsStringOrNull()) {
          throw new JsonException(
            $"Unexpected JSON value {typeValue.ValueKind}. Was expecting a string for the {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Name)} property."
          );
        }

        if (!element.TryGetProperty(nameof(HealthCheckModel.Description), ignoreCase: true, out var descriptionElement)) {
          throw new JsonException($"The {nameof(HealthCheckModel.Description)} property is required.");
        }
        if (!descriptionElement.IsStringOrNull()) {
          throw new JsonException(
            $"Unexpected JSON value {typeValue.ValueKind}. Was expecting a string for the {nameof(HealthCheckModel)}.{nameof(HealthCheckModel.Description)} property."
          );
        }

        return new HealthCheckModel(
          nameElement.GetString() ?? throw new JsonException($"The {nameof(HealthCheckModel.Name)} property is required."),
          descriptionElement.GetString() ?? throw new JsonException($"The {nameof(HealthCheckModel.Description)} property is required."),
          type,
          definition
        );
      case JsonValueKind.Array:
      case JsonValueKind.String:
      case JsonValueKind.Number:
      case JsonValueKind.True:
      case JsonValueKind.False:
      default:
        throw new ArgumentOutOfRangeException();
    }
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
        JsonSerializer.Serialize(writer, (PrometheusHealthCheckDefinition)value.Definition, options);
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
