using System;
using System.IO;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class ConfigurationHelperTests {

  [Fact]
  public async Task GetServiceHierarchyConfigurationFromJson_ValidConfiguration_ReturnsConfigurationObject() {
    const String jsonFilePath = "test-inputs/valid-service-config-1.json";
    await using var jsonStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read);

    var configuration = await ConfigurationHelper.GetServiceHierarchyConfigurationFromJsonAsync(
      jsonStream,
      cancellationToken: default);

    Assert.Equal(expected: 4, configuration.Services.Count);
    Assert.Equal(expected: 2, configuration.RootServices.Count);
  }

  // TODO(BATAPI-242): These tests need to be reworked after the code issues causing the incorrect behavior are fixed.
  // The first one should not deserialize, but it does. It should be telling us the service name is missing.
  // The second should tell us the services field is missing. The JSON serializer in System.Text.Json does not
  // appear to be respect the attributes from System.ComponentModel.DataAnnotations on the config records.
  [Theory]
  [InlineData(
    "test-inputs/invalid-service-config-missing-keys.json",
    "Invalid JSON service configuration: One or more of the specified root services do not exist in the services array.")]
  [InlineData(
    "test-inputs/invalid-service-config-empty.json",
    "Invalid JSON service configuration: Value cannot be null. (Parameter 'source')")]
  public async Task GetServiceHierarchyConfigurationFromJson_InvalidConfiguration_ThrowsInvalidConfigurationException(
    String testInputFilePath,
    String expectedExceptionMessage) {

    await using var jsonStream = new FileStream(testInputFilePath, FileMode.Open, FileAccess.Read);

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(() =>
      ConfigurationHelper.GetServiceHierarchyConfigurationFromJsonAsync(jsonStream));

    Assert.StartsWith(expectedExceptionMessage, exception.Message);
  }

}
