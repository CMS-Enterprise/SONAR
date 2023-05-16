using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class ConfigurationHelperTests {

  [Fact]
  public async Task GetServiceHierarchyConfigurationFromJson_ValidConfiguration_ReturnsConfigurationObject() {
    const String jsonFilePath = "test-inputs/valid-service-config-1.json";

    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { jsonFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>()
    );

    var configuration = await configHelper.LoadAndValidateJsonServiceConfigAsync(CancellationToken.None);

    Assert.True(configuration.TryGetValue("test", out var tenantConfig));

    Assert.Equal(expected: 4, tenantConfig.Services.Count);
    Assert.Equal(expected: 2, tenantConfig.RootServices.Count);
  }

  [Theory]
  [InlineData(
    "test-inputs/invalid-service-config-empty.json", new[] {
      "Services: The Services field is required.",
      "RootServices: The RootServices field is required."
    })]
  [InlineData(
    "test-inputs/invalid-service-config-constraint-violations.json", new[] {
      "RootServices: One or more of the specified root services do not exist in the services array.",
      "Services[0].Name: The Name field is required.",
      "Services[0].HealthChecks[0].Name: The field Name must match the regular expression '^[0-9a-zA-Z_-]+$'."
    })]
  public async Task
    GetServiceHierarchyConfigurationFromJson_PostSerializationValidationError_ThrowsInvalidConfigurationException(
      String testInputFilePath,
      String[] expectedValidationErrors) {

    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { testInputFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>()
    );

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(() =>
      configHelper.LoadAndValidateJsonServiceConfigAsync(CancellationToken.None)
    );

    Assert.Equal("Invalid JSON service configuration: One or more validation errors occurred.", exception.Message);
    Assert.True(exception.Data.Contains("errors"));
    Assert.IsType<List<ValidationResult>>(exception.Data["errors"]);
    Assert.Equal(expected: expectedValidationErrors.Length, ((List<ValidationResult>)exception.Data["errors"]!).Count);
    foreach (var expectedError in expectedValidationErrors) {
      Assert.Contains(
        (List<ValidationResult>)exception.Data["errors"]!,
        filter: error => expectedError.Equals(error.ErrorMessage));
    }
  }

  /// <summary>
  /// Some of our validation errors in the agent will occur _during_ deserialization, manifested by an exception thrown
  /// from one of the custom JsonConverters (<see cref="HealthCheckModelJsonConverter"/>, <see cref="HttpHealthCheckConditionJsonConverter"/>).
  /// These result in the object not actually getting deserialized, and we want to make sure our deserialization helper
  /// catches these errors, and converts them to <see cref="InvalidConfigurationException"/>s.
  /// </summary>
  [Theory]
  [InlineData("test-inputs/invalid-service-config-missing-keys-1.json",
    "Invalid JSON service configuration: The Type property is required.")]
  [InlineData("test-inputs/invalid-service-config-missing-keys-2.json",
    "Invalid JSON service configuration: The Name property is required.")]
  public async Task
    GetServiceHierarchyConfigurationFromJson_DuringSerializationValidationError_ThrowsInvalidConfigurationException(
      String testInputFilePath,
      String expectedExceptionMessage) {

    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { testInputFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>()
    );

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(() =>
      configHelper.LoadAndValidateJsonServiceConfigAsync(CancellationToken.None)
    );

    Assert.Equal(expectedExceptionMessage, exception.Message);
  }

}
