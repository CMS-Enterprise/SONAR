using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Models;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Cms.BatCave.Sonar.Tests;

public class ServiceConfigurationTagsTest {
  private readonly ITestOutputHelper _testOutputHelper;

  private static readonly JsonSerializerOptions DefaultOptions = new() {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private const String TestTagKey = "test-key";
  private const String TestTagVal = "test-value";
  private const String TestNullTagKey = "test-null-key";

  private static readonly String ValidServiceConfiguration =
    JsonSerializer.Serialize(
      new {
        name = "test name",
        displayName = "test display name",
        tags = new Dictionary<String, String?>() {
          { TestTagKey, TestTagVal },
          { TestNullTagKey, null }
        }
      });

  private static readonly String ValidServiceHierarchyConfiguration =
    JsonSerializer.Serialize(
      new {
        services = ImmutableList<ServiceConfiguration>.Empty,
        rootServices = ImmutableList<ServiceConfiguration>.Empty,
        tags = new Dictionary<String, String?>() {
          { TestTagKey, TestTagVal },
          { TestNullTagKey, null }
        }
      });

  public ServiceConfigurationTagsTest(ITestOutputHelper testOutputHelper) {
    this._testOutputHelper = testOutputHelper;
  }

  [Fact]
  public void Deserialize_ServiceConfig_NonNullTag_Success() {
    var result =
      JsonSerializer.Deserialize<ServiceConfiguration>(ValidServiceConfiguration, DefaultOptions);

    Assert.NotNull(result);
    Assert.NotNull(result.Tags);

    var testVal = Assert.Contains(TestTagKey, result.Tags);
    Assert.Equal(expected: TestTagVal, actual: testVal);
  }

  [Fact]
  public void Deserialize_ServiceConfig_NullTag_Success() {
    var result =
      JsonSerializer.Deserialize<ServiceConfiguration>(ValidServiceConfiguration, DefaultOptions);

    Assert.NotNull(result);
    Assert.NotNull(result.Tags);

    var testNullVal = Assert.Contains(TestNullTagKey, result.Tags);
    Assert.Null(testNullVal);
  }

  [Fact]
  public void Deserialize_ServiceHierarchyConfig_NonNullTag_Success() {
    var result =
      JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(ValidServiceHierarchyConfiguration, DefaultOptions);

    Assert.NotNull(result);
    Assert.NotNull(result.Tags);

    var testVal = Assert.Contains(TestTagKey, result.Tags);
    Assert.Equal(expected: TestTagVal, actual: testVal);
  }

  [Fact]
  public void Deserialize_ServiceHierarchyConfig_NullTag_Success() {
    var result =
      JsonSerializer.Deserialize<ServiceHierarchyConfiguration>(ValidServiceHierarchyConfiguration, DefaultOptions);

    Assert.NotNull(result);
    Assert.NotNull(result.Tags);

    var testNullVal = Assert.Contains(TestNullTagKey, result.Tags);
    Assert.Null(testNullVal);
  }
}
