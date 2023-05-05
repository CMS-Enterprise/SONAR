using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Models;

public class ServiceHierarchyConfigurationTests {

  [Fact]
  public async Task Validate_ConfigurationIsValid_MethodReturnsQuietly() {
    await GetServiceHierarchyConfigurationFromJsonAsync("test-inputs/valid-service-config-1.json");
  }

  [Theory]
  [InlineData(
    "test-inputs/invalid-service-config-duplicate-service-names.json",
    "The specified list of services contained multiple services with the same name.")]
  [InlineData(
    "test-inputs/invalid-service-config-undefined-root-services.json",
    "One or more of the specified root services do not exist in the services array.")]
  [InlineData(
    "test-inputs/invalid-service-config-undefined-child-services.json",
    "One or more of the specified services contained a reference to a child service that did not exist in the services array.")]
  public async Task Validate_ServiceNameIsDuplicated_ThrowsInvalidConfigurationException(
    String testInputFilePath,
    String expectedExceptionMessage) {

    var configuration = await GetServiceHierarchyConfigurationFromJsonAsync(testInputFilePath);

    var exception = Assert.Throws<InvalidConfigurationException>(configuration.Validate);

    Assert.StartsWith(expectedExceptionMessage, exception.Message);
  }

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
