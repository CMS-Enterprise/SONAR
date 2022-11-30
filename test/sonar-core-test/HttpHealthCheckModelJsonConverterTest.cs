using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Xunit;

namespace Cms.BatCave.Sonar.SonarCoreTest;

public class HttpHealthCheckModelJsonConverterTest {
  private static readonly JsonSerializerOptions DefaultOptions = new() {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private const String TestHealthCheckName = "Example Http Health Check Name";
  private const String TestHealthCheckDescription = "Test Description";
  // Definition test variables
  private static readonly Uri TestUrl = new Uri("http://localhost:8080/test/url");
  private const Boolean TestFollowRedirects = true;
  private const String TestAuthHeader = "testAuthHeader";
  // Properties for first Condition in Conditions array.
  private const HealthStatus TestHealthStatusDegraded = HealthStatus.Degraded;
  private static readonly TimeSpan TestHttpResponseTime = TimeSpan.Parse("1.23:00:01");
  private const HttpHealthCheckConditionType TestHttpConditionTypeResponse = HttpHealthCheckConditionType.HttpResponseTime;
  // Properties for the second Condition in Conditions array.
  private const HealthStatus TestHealthStatusOnline = HealthStatus.Online;
  private static readonly UInt16[] TestStatusCodes = { 200, 500, 404 };
  private const HttpHealthCheckConditionType TestHttpConditionTypeStatus = HttpHealthCheckConditionType.HttpStatusCode;
  private static readonly String ValidHttpCheck =
    JsonSerializer.Serialize(
      new {
        name = TestHealthCheckName,
        description = TestHealthCheckDescription,
        type = "HttpRequest",
        definition = new {
          Url = TestUrl,
          FollowRedirects = TestFollowRedirects,
          AuthorizationHeader = TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = TestHealthStatusDegraded.ToString(),
              ResponseTime = TestHttpResponseTime.ToString(),
              Type = TestHttpConditionTypeResponse.ToString()
            },
            new {
              Status = TestHealthStatusOnline.ToString(),
              StatusCodes = TestStatusCodes,
              Type = TestHttpConditionTypeStatus.ToString()
            },
          }
        }
      });

  [Fact]
  public void Deserialize_HttpCheck_Success() {
    var result = JsonSerializer.Deserialize<HealthCheckModel>(ValidHttpCheck, DefaultOptions);

    Assert.NotNull(result);
    // Test HealthCheckModel base properties.
    Assert.Equal(TestHealthCheckName, result.Name);
    Assert.Equal(TestHealthCheckDescription, result.Description);
    Assert.Equal(HealthCheckType.HttpRequest, result.Type);
    // Test Definition.
    var definition = Assert.IsType<HttpHealthCheckDefinition>(result.Definition);
    Assert.Equal(TestUrl, definition.Url);
    Assert.Equal(TestFollowRedirects, definition.FollowRedirects);
    Assert.Equal(TestAuthHeader, definition.AuthorizationHeader);
    // Test HttpHealthCheck Conditions.
    var conditions = Assert.IsType<HttpHealthCheckCondition[]>(definition.Conditions);
    // Assert that Conditions array has only 2 elements.
    Assert.Equal(2, conditions.Length);
    // Test first element of Conditions array (Http response time).
    var res1 = Assert.IsType<ResponseTimeCondition>(conditions[0]);
    Assert.Equal(TestHealthStatusDegraded, res1.Status);
    Assert.Equal(TestHttpResponseTime, res1.ResponseTime);
    Assert.Equal(TestHttpConditionTypeResponse, res1.Type);
    // Test second element of Conditions array (Status code).
    var res2 = Assert.IsType<StatusCodeCondition>(conditions[1]);
    Assert.Equal(TestHealthStatusOnline, res2.Status);
    Assert.True(TestStatusCodes.SequenceEqual(res2.StatusCodes));
    Assert.Equal(TestStatusCodes, res2.StatusCodes);
    Assert.Equal(TestHttpConditionTypeStatus, res2.Type);
  }

  [Fact]
  public void SerializeRoundTrip_HttpCheck_Success() {
    var conditions = new HttpHealthCheckCondition[] {
      new ResponseTimeCondition(ResponseTime: TestHttpResponseTime, TestHealthStatusDegraded),
      new StatusCodeCondition(TestStatusCodes, TestHealthStatusOnline)
    };
    HttpHealthCheckDefinition originalDefinition;
    var original = new HealthCheckModel(
      TestHealthCheckName,
      TestHealthCheckDescription,
      HealthCheckType.HttpRequest,
      originalDefinition = new HttpHealthCheckDefinition(
        TestUrl,
        conditions,
        TestFollowRedirects,
        TestAuthHeader));

    var serializedModel = JsonSerializer.Serialize(original, DefaultOptions);

    Assert.NotNull(serializedModel);

    var deserializedModel = JsonSerializer.Deserialize<HealthCheckModel>(serializedModel, DefaultOptions);

    Assert.NotNull(deserializedModel);
    // Test Health Check Model base properties.
    Assert.Equal(original.Name, deserializedModel.Name);
    Assert.Equal(original.Description, deserializedModel.Description);
    Assert.Equal(original.Type, deserializedModel.Type);
    // Test Http Health Check Definition Properties
    var deserializedDef = Assert.IsType<HttpHealthCheckDefinition>(deserializedModel.Definition);
    Assert.Equal(originalDefinition.Url, deserializedDef.Url);
    Assert.Equal(originalDefinition.AuthorizationHeader, deserializedDef.AuthorizationHeader);
    Assert.Equal(originalDefinition.FollowRedirects, deserializedDef.FollowRedirects);

    // Test Conditions Array
    var originalRespElement = Assert.IsType<ResponseTimeCondition>(originalDefinition.Conditions[0]);
    var originalStatusElement = Assert.IsType<StatusCodeCondition>(originalDefinition.Conditions[1]);
    var dsRespElement = Assert.IsType<ResponseTimeCondition>(deserializedDef.Conditions[0]);
    var dsStatusElement = Assert.IsType<StatusCodeCondition>(deserializedDef.Conditions[1]);

    Assert.NotNull(dsRespElement);
    Assert.NotNull(dsStatusElement);

    Assert.Equal(originalRespElement.ResponseTime, dsRespElement.ResponseTime);
    Assert.Equal(originalRespElement.Status, dsRespElement.Status);
    Assert.Equal(originalRespElement.Type, dsRespElement.Type);

    Assert.True(originalStatusElement.StatusCodes.SequenceEqual(dsStatusElement.StatusCodes));
    Assert.Equal(originalStatusElement.Status, dsStatusElement.Status);
    Assert.Equal(originalStatusElement.Type, dsStatusElement.Type);
  }

  [Fact]
  public void Deserialize_MissingType_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = TestUrl,
          FollowRedirects = TestFollowRedirects,
          AuthorizationHeader = TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = TestHealthStatusDegraded,
              ResponseTime = TestHttpResponseTime.ToString(),
              // Type property is missing
            },
            new {
              Status = TestHealthStatusOnline,
              StatusCodes = TestStatusCodes.ToString(),
              // Type property is missing
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, DefaultOptions)
      );
  }

  [Fact]
  public void Deserialize_InvalidTypeValue_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = TestUrl,
          FollowRedirects = TestFollowRedirects,
          AuthorizationHeader = TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = TestHealthStatusDegraded,
              ResponseTime = TestHttpResponseTime.ToString(),
              // Invalid type
              Type = "invalid type"
            },
            new {
              Status = TestHealthStatusOnline,
              StatusCodes = TestStatusCodes.ToString(),
              // Invalid type
              Type = "invalid type"
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingResponseTime_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = TestUrl,
          FollowRedirects = TestFollowRedirects,
          AuthorizationHeader = TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = TestHealthStatusDegraded,
              // Missing Response Time property
              Type = TestHttpConditionTypeResponse
            },
            new {
              Status = TestHealthStatusOnline,
              StatusCodes = TestStatusCodes.ToString(),
              Type = TestHttpConditionTypeStatus
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingStatusCodes_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = TestUrl,
          FollowRedirects = TestFollowRedirects,
          AuthorizationHeader = TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = TestHealthStatusDegraded,
              ResponseTime = TestHttpResponseTime.ToString(),
              Type = TestHttpConditionTypeResponse
            },
            new {
              Status = TestHealthStatusOnline,
              // Missing Status Code property
              Type = TestHttpConditionTypeStatus
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, DefaultOptions)
    );
  }
};




