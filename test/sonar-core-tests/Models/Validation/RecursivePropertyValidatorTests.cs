using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models.Validation;

public class RecursivePropertyValidatorTests {

  private readonly RecursivePropertyValidator _validator;

  public RecursivePropertyValidatorTests() => this._validator = new RecursivePropertyValidator();

  [Fact]
  public async Task TryValidateObjectProperties_ValidServiceHierarchyConfiguration_NoErrorsAreReported() {
    var configuration = await GetServiceHierarchyConfigurationFromJsonAsync("test-inputs/valid-service-config-1.json");
    var validationResults = new List<ValidationResult>();

    var isValid = this._validator.TryValidateObjectProperties(configuration, validationResults);

    Assert.True(isValid);
    Assert.Empty(validationResults);
  }

  [Theory]
  [InlineData("test-inputs/invalid-service-config-empty.json", new[] {
    "Services: The Services field is required.",
    "RootServices: The RootServices field is required."
  })]
  [InlineData("test-inputs/invalid-service-config-duplicate-service-names.json", new[] {
    "Services: The specified list of services contained multiple services with the same name."
  })]
  [InlineData("test-inputs/invalid-service-config-undefined-child-services.json", new[] {
    "Services: One or more of the specified services contained a reference to a child service that did not exist in the services array."
  })]
  [InlineData("test-inputs/invalid-service-config-undefined-root-services.json", new[] {
    "RootServices: One or more of the specified root services do not exist in the services array."
  })]
  [InlineData("test-inputs/invalid-service-config-attribute-constraint-violations.json", new[] {
    "RootServices: The RootServices field is required.",
    "Services[0].Name: The field Name must be a string with a maximum length of 100.",
    "Services[0].HealthChecks[0].Name: The field Name must be a string with a maximum length of 100.",
    "Services[1].Name: The Name field is required.",
    "Services[1].DisplayName: The DisplayName field is required.",
    "Services[2].Name: The field Name must be a string with a maximum length of 100.",
    "Services[2].Name: The field Name must match the regular expression '^[0-9a-zA-Z_-]+$'.",
    "Services[2].HealthChecks[0].Name: The field Name must be a string with a maximum length of 100.",
    "Services[2].HealthChecks[0].Name: The field Name must match the regular expression '^[0-9a-zA-Z_-]+$'."
  })]
  public async Task TryValidateObjectProperties_InvalidServiceHierarchyConfiguration_AllErrorsAreReported(
    String testInputFilePath,
    String[] expectedValidationErrors) {

    var configuration = await GetServiceHierarchyConfigurationFromJsonAsync(testInputFilePath);
    var validationResults = new List<ValidationResult>();

    var isValid = this._validator.TryValidateObjectProperties(configuration, validationResults);

    Assert.False(isValid);
    Assert.Equal(expected: expectedValidationErrors.Length, validationResults.Count);
    foreach (var expectedError in expectedValidationErrors) {
      Assert.Contains(validationResults, filter: result => result.ErrorMessage!.Equals(expectedError));
    }
  }

  /// <summary>
  /// Test helper method for deserializing test data files.
  /// </summary>
  /// <param name="jsonFilePath">The path to the JSON file to deserialize.</param>
  /// <returns>The deserialized object.</returns>
  /// <exception cref="InvalidConfigurationException">If the deserialized object is null.</exception>
  private static async Task<ServiceHierarchyConfiguration> GetServiceHierarchyConfigurationFromJsonAsync(String jsonFilePath) {
    var serializerOptions = new JsonSerializerOptions {
      PropertyNameCaseInsensitive = true,
      Converters = { new JsonStringEnumConverter() }
    };

    await using var jsonStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read);

    return await JsonSerializer.DeserializeAsync<ServiceHierarchyConfiguration>(jsonStream, serializerOptions) ??
      throw new InvalidConfigurationException("Invalid JSON service configuration: deserialized object is null.");
  }
}
