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
    var errorReportsHelper = new ErrorReportsHelper(() => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ErrorReportsHelper>>());
    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { jsonFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>(),
      errorReportsHelper
    );

    var configuration = await configHelper.LoadAndValidateJsonServiceConfigAsync(
      "testEnv",
      CancellationToken.None);

    Assert.True(configuration.TryGetValue("test", out var tenantConfig));

    Assert.Equal(expected: 4, tenantConfig.Services.Count);
    Assert.Equal(expected: 2, tenantConfig.RootServices.Count);
  }

  /// <summary>
  /// Some of our validation errors in the agent will occur _post_ serialization, manifested by an exception thrown
  /// from the ServiceHierarchyConfiguration validator. We want to make sure the service hierarchy configuration
  /// validator catches these errors gracefully and results in the invalid configuration NOT being added to
  /// the database.
  /// </summary>
  /// <param name="testInputFilePath"></param>
  [Theory]
  [InlineData("test-inputs/invalid-service-config-empty.json")]
  [InlineData("test-inputs/invalid-service-config-constraint-violations.json")]
  [InlineData("test-inputs/invalid-service-config-missing-keys-1.json")]
  [InlineData("test-inputs/invalid-service-config-missing-keys-2.json")]
  public async Task
    GetServiceHierarchyConfigurationFromJson_PostSerializationValidationError_ThrowsInvalidConfigurationException(
      String testInputFilePath) {

    var errorReportsHelper = new ErrorReportsHelper(() => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ErrorReportsHelper>>());
    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { testInputFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>(),
      errorReportsHelper
    );

    var configuration = await configHelper.LoadAndValidateJsonServiceConfigAsync(
      "testEnv",
      CancellationToken.None);

    Assert.Equal(expected: 0, configuration.Count);
    Assert.False(configuration.TryGetValue("test", out var tenantConfig));
  }

  /// <summary>
  /// Some of our validation errors in the agent will occur _during_ deserialization, manifested by an exception thrown
  /// from one of the custom JsonConverters (<see cref="HealthCheckModelJsonConverter"/>, <see cref="HttpHealthCheckConditionJsonConverter"/>).
  /// These result in the object not actually getting deserialized, and we want to make sure our deserialization helper
  /// catches these errors, and converts them to <see cref="InvalidConfigurationException"/>s.
  /// </summary>
  [Theory]
  [InlineData("test-inputs/invalid-service-config-json-format.json",
    InvalidConfigurationErrorType.InvalidJson)]
  public async Task
    GetServiceHierarchyConfigurationFromJson_DuringSerializationValidationError_ThrowsInvalidConfigurationException(
      String testInputFilePath,
      InvalidConfigurationErrorType expectedExceptionType) {

    var errorReportsHelper = new ErrorReportsHelper(() => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ErrorReportsHelper>>());
    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { testInputFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>(),
      errorReportsHelper
    );

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(() =>
      configHelper.LoadAndValidateJsonServiceConfigAsync(
        "test",
        CancellationToken.None)
    );

    Assert.Equal(expectedExceptionType, exception.ErrorType);
  }
}
