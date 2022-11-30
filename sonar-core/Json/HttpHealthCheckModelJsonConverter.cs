
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Json;

public class HttpHealthCheckConditionJsonConverter : JsonConverter<HttpHealthCheckCondition> {
  public override HttpHealthCheckCondition? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options) {
    var originalReader = reader;
    var document = JsonDocument.ParseValue(ref reader);
    var element = document.RootElement;

    switch (element.ValueKind) {
      case JsonValueKind.Undefined:
      case JsonValueKind.Null:
        return null;
      case JsonValueKind.Object:
        if (!element.TryGetProperty(nameof(HttpHealthCheckCondition.Type), ignoreCase: true, out var typeValue) ||
            typeValue.IsNullOrUndefined()) {

          throw new JsonException(
            $"The {nameof(HttpHealthCheckCondition.Type)} property is required."
          );
        }

        if (!Enum.TryParse<HttpHealthCheckConditionType>(typeValue.GetString(), ignoreCase: true, out var type)) {
          throw new JsonException(
            $"Invalid value for property {nameof(HttpHealthCheckCondition)}.{nameof(HttpHealthCheckCondition.Type)}: {typeValue.GetString()}");
        }

        switch (type) {
          case HttpHealthCheckConditionType.HttpStatusCode:
            if (!element.TryGetProperty(nameof(StatusCodeCondition.StatusCodes), ignoreCase: true,
                  out var statusCodeElement)) {
              throw new JsonException(
                $"The {nameof(StatusCodeCondition.StatusCodes)} property is required.");
            }
            return JsonSerializer.Deserialize<StatusCodeCondition>(ref originalReader, options);
          case HttpHealthCheckConditionType.HttpResponseTime:
            if (!element.TryGetProperty(nameof(ResponseTimeCondition.ResponseTime), ignoreCase: true,
                  out var responseTimeElement)) {
              throw new JsonException(
                $"The {nameof(ResponseTimeCondition.ResponseTime)} property is required.");
            }
            return JsonSerializer.Deserialize<ResponseTimeCondition>(ref originalReader, options);
          default:
            throw new JsonException($"Invalid HTTP Health Check Condition Type: {type}");
        }
      case JsonValueKind.Array:
      case JsonValueKind.String:
      case JsonValueKind.Number:
      case JsonValueKind.True:
      case JsonValueKind.False:
      default:
        throw new JsonException($"Unexpected property type: {element.ValueKind}");
    }
  }

  public override void Write(Utf8JsonWriter writer, HttpHealthCheckCondition value, JsonSerializerOptions options) {
    switch (value.Type) {
      case HttpHealthCheckConditionType.HttpStatusCode:
        JsonSerializer.Serialize(writer, (StatusCodeCondition)value, options);
        break;
      case HttpHealthCheckConditionType.HttpResponseTime:
        JsonSerializer.Serialize(writer, (ResponseTimeCondition)value, options);
        break;
      default:
        throw new NotSupportedException(
          $"Unable to deserialize definition. Unsupported health check type: {value.Type}");
    }
  }
}
