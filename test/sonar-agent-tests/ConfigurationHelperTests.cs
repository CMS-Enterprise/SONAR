using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Helpers;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Json;
using Cms.BatCave.Sonar.Models;
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
  /// catches these errors, and generates an <see cref="ErrorReportDetails"/>.
  /// </summary>
  [Theory]
  [InlineData("test-inputs/invalid-service-config-json-format.json")]
  public async Task
    GetServiceHierarchyConfigurationFromJson_DuringSerializationValidationError_ThrowsInvalidConfigurationException(
      String testInputFilePath) {

    var errorReportsHelper = new Mock<IErrorReportsHelper>(MockBehavior.Loose);
    errorReportsHelper.Setup(er =>
      er.CreateErrorReport("test", It.IsAny<ErrorReportDetails>(), It.IsAny<CancellationToken>()));

    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { testInputFilePath }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>(),
      errorReportsHelper.Object);

    var validatedConfig = await configHelper.LoadAndValidateJsonServiceConfigAsync(
      "test",
      CancellationToken.None);

    Assert.Empty(validatedConfig);
    errorReportsHelper.Verify(er => er.CreateErrorReport("test", It.IsAny<ErrorReportDetails>(), It.IsAny<CancellationToken>()));
  }

  /// <summary>
  /// Some of our validation errors in the agent will occur _during_ deserialization, manifested by an exception thrown
  /// from one of the custom JsonConverters (<see cref="JsonServiceConfigSerializer"/>).
  /// These result in the object not actually getting deserialized, and we want to make sure our deserialization helper
  /// catches these errors, and converts them to <see cref="InvalidConfigurationException"/>s.
  /// </summary>
  [Theory]
  [InlineData("test-inputs/invalid-service-config-json-format.json",
    InvalidConfigurationErrorType.InvalidJson)]
  public async Task
    GetServiceHierarchyConfigurationFromJson_DuringDeserializationLocalFileError_ThrowsInvalidConfigurationException(
      String testInputFilePath,
      InvalidConfigurationErrorType expectedExceptionType) {

    var configSource = new LocalFileServiceConfigSource(tenant: "test", filePaths: new[] {
      testInputFilePath
    });

    var exception = await Assert.ThrowsAsync<InvalidConfigurationException>(async () =>
      await configSource.GetConfigurationLayersAsync(tenant: "test", CancellationToken.None).SingleAsync()
    );

    Assert.Equal(expectedExceptionType, exception.ErrorType);
  }

  /// <summary>
  /// When validation errors occur, the agent will generate an Error Report containing the type of error
  /// and the list of validation pertaining to the configuration loaded.
  /// </summary>
  [Theory]
  [InlineData("test-inputs/invalid-service-config-constraint-violations.json",
    InvalidConfigurationErrorType.DataValidationError)]
  public async Task
    GetServiceHierarchyConfigurationFromJson_DuringDeserializationLocalFileError_LogAdditionalInformation(
      String testInputFilePath,
      InvalidConfigurationErrorType expectedExceptionType) {
    var configSource = new LocalFileServiceConfigSource(tenant: "test", filePaths: new[] { testInputFilePath });
    var config = await configSource.GetConfigurationLayersAsync(tenant: "test", cancellationToken: default)
      .SingleAsync();

    try {
      ServiceConfigValidator.ValidateServiceConfig(config);
    } catch (InvalidConfigurationException e) {

      // Validate Data is not Empty
      Assert.NotEqual(e.Data, ImmutableDictionary<String, Object?>.Empty);

      // Match String
      var expectedErrorMessage = $"{nameof(InvalidConfigurationErrorType)}: {e.ErrorType.ToString()}";
      Assert.Contains(expectedErrorMessage, e.ReadValidationResults());
      Assert.Equal(expectedExceptionType, e.ErrorType);
    }
  }

  [Fact]
  public async Task MergeConfigurationWithTags_Success() {
    const String jsonFilePath1 = "test-inputs/merge-tags-config1.json";
    const String jsonFilePath2 = "test-inputs/merge-tags-config2.json";
    var errorReportsHelper = new ErrorReportsHelper(() => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ErrorReportsHelper>>());
    var configHelper = new ConfigurationHelper(
      new LocalFileServiceConfigSource("test", new[] { jsonFilePath1, jsonFilePath2 }),
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>()),
      Mock.Of<ILogger<ConfigurationHelper>>(),
      errorReportsHelper
    );

    var configuration = await configHelper.LoadAndValidateJsonServiceConfigAsync(
      "testEnv",
      CancellationToken.None);

    Assert.True(configuration.TryGetValue("test", out var tenantConfig));

    // Tenant tag merge tests
    Assert.NotNull(tenantConfig);
    Assert.NotNull(tenantConfig.Tags);
    var tenantTags = tenantConfig.Tags;
    var unchangedTenantTag = Assert.Contains("tenant-tag", tenantTags);
    var updatedTenantTag = Assert.Contains("tenant-tag-merge", tenantTags);
    var newTenantTag = Assert.Contains("new-tenant-tag", tenantTags);
    // Test case where tenant tag exists in left hand config and remains unchanged
    Assert.Equal(expected: "tenant-tag-val", actual: unchangedTenantTag);
    // Test case where tenant tag exists in left hand config and updated by right hand config
    Assert.Equal(expected: "updated-tenant-tag", actual: updatedTenantTag);
    // Test case where tenant tag is added by right hand config
    Assert.Equal(expected: "new-tenant-tag-val", actual: newTenantTag);

    // Service tag merge tests
    var rootService1 = tenantConfig.Services.FirstOrDefault(
      svc => svc.Name == $"service-with-health-checks-and-children");
    var rootService2 = tenantConfig.Services.FirstOrDefault(
      svc => svc.Name == $"service-with-only-children");
    Assert.NotNull(rootService1);
    Assert.NotNull(rootService2);

    var rootService1Tags = rootService1.Tags;
    var rootService2Tags = rootService2.Tags;

    Assert.NotNull(rootService1Tags);
    Assert.NotNull(rootService2Tags);

    // Test case where tag exists in both configs, gets updated by the right hand config
    var updatedServiceTag = Assert.Contains("service-tag-merge", rootService1Tags);
    Assert.Equal(expected: "service-tag-updated", actual: updatedServiceTag);

    // Test case where tag present in left hand config and persists after merge
    var unchangedServiceTag = Assert.Contains("root-service-tag", rootService1Tags);
    Assert.Equal(expected: "test-val", actual: unchangedServiceTag);

    // Test case where tag is not present in left hand config but is added in right hand config
    var newServiceTag = Assert.Contains("new-service-tag", rootService2Tags);
    Assert.Equal(expected: "new-service-tag-val", actual: newServiceTag);
  }
}
