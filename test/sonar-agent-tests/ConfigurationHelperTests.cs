using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Json;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class ConfigurationHelperTests {

  [Fact]
  public async Task GetServiceHierarchyConfigurationFromJson_ValidConfiguration_ReturnsConfigurationObject() {
    const String jsonFilePath = "test-inputs/valid-service-config-1.json";
    await using var jsonStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read);

    var configuration = await ConfigurationHelper.GetServiceHierarchyConfigurationFromJsonAsync(jsonStream);

    Assert.Equal(expected: 4, configuration.Services.Count);
    Assert.Equal(expected: 2, configuration.RootServices.Count);
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
  public async Task GetServiceHierarchyConfigurationFromJson_PostSerializationValidationError_ThrowsInvalidConfigurationException(
    String testInputFilePath,
    String[] expectedValidationErrors) {

    await using var jsonStream = new FileStream(testInputFilePath, FileMode.Open, FileAccess.Read);

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(() =>
      ConfigurationHelper.GetServiceHierarchyConfigurationFromJsonAsync(jsonStream));

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
  public async Task GetServiceHierarchyConfigurationFromJson_DuringSerializationValidationError_ThrowsInvalidConfigurationException(
      String testInputFilePath,
      String expectedExceptionMessage) {

    await using var jsonStream = new FileStream(testInputFilePath, FileMode.Open, FileAccess.Read);

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(() =>
      ConfigurationHelper.GetServiceHierarchyConfigurationFromJsonAsync(jsonStream));

    Assert.Equal(expectedExceptionMessage, exception.Message);
  }

}
