using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public static class JsonServiceConfigSerializer {
  public static readonly JsonSerializerOptions ConfigSerializerOptions = new() {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  public static ServiceHierarchyConfiguration Deserialize(String data) {
    ServiceHierarchyConfiguration? configuration;
    try {
      configuration =
        JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(data, ConfigSerializerOptions);
    } catch (Exception e) when (e is JsonException or NotSupportedException) {
      throw new InvalidConfigurationException(
        message: $"Invalid JSON service configuration: {e.Message}",
        InvalidConfigurationErrorType.InvalidJson,
        e
      );
    }

    if (configuration == null) {
      throw new InvalidConfigurationException(
        "Invalid JSON service configuration: Deserialized object is null.",
        InvalidConfigurationErrorType.TopLevelNull
      );
    }

    return configuration;
  }
}
