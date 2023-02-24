using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

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
        name = HttpHealthCheckModelJsonConverterTest.TestHealthCheckName,
        description = HttpHealthCheckModelJsonConverterTest.TestHealthCheckDescription,
        type = "HttpRequest",
        definition = new {
          Url = HttpHealthCheckModelJsonConverterTest.TestUrl,
          FollowRedirects = HttpHealthCheckModelJsonConverterTest.TestFollowRedirects,
          AuthorizationHeader = HttpHealthCheckModelJsonConverterTest.TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded.ToString(),
              ResponseTime = HttpHealthCheckModelJsonConverterTest.TestHttpResponseTime.ToString(),
              Type = HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeResponse.ToString()
            },
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline.ToString(),
              StatusCodes = HttpHealthCheckModelJsonConverterTest.TestStatusCodes,
              Type = HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeStatus.ToString()
            },
          }
        }
      });

  [Fact]
  public void Deserialize_HttpCheck_Success() {
    var result = JsonSerializer.Deserialize<HealthCheckModel>(HttpHealthCheckModelJsonConverterTest.ValidHttpCheck, HttpHealthCheckModelJsonConverterTest.DefaultOptions);

    Assert.NotNull(result);
    // Test HealthCheckModel base properties.
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHealthCheckName, result.Name);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHealthCheckDescription, result.Description);
    Assert.Equal(HealthCheckType.HttpRequest, result.Type);
    // Test Definition.
    var definition = Assert.IsType<HttpHealthCheckDefinition>(result.Definition);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestUrl, definition.Url);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestFollowRedirects, definition.FollowRedirects);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestAuthHeader, definition.AuthorizationHeader);
    // Test HttpHealthCheck Conditions.
    var conditions = Assert.IsType<HttpHealthCheckCondition[]>(definition.Conditions);
    // Assert that Conditions array has only 2 elements.
    Assert.Equal(2, conditions.Length);
    // Test first element of Conditions array (Http response time).
    var res1 = Assert.IsType<ResponseTimeCondition>(conditions[0]);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded, res1.Status);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHttpResponseTime, res1.ResponseTime);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeResponse, res1.Type);
    // Test second element of Conditions array (Status code).
    var res2 = Assert.IsType<StatusCodeCondition>(conditions[1]);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline, res2.Status);
    Assert.True(HttpHealthCheckModelJsonConverterTest.TestStatusCodes.SequenceEqual(res2.StatusCodes));
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestStatusCodes, res2.StatusCodes);
    Assert.Equal(HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeStatus, res2.Type);
  }

  [Fact]
  public void SerializeRoundTrip_HttpCheck_Success() {
    var conditions = new HttpHealthCheckCondition[] {
      new ResponseTimeCondition(ResponseTime: HttpHealthCheckModelJsonConverterTest.TestHttpResponseTime, HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded),
      new StatusCodeCondition(HttpHealthCheckModelJsonConverterTest.TestStatusCodes, HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline)
    };
    HttpHealthCheckDefinition originalDefinition;
    var original = new HealthCheckModel(
      HttpHealthCheckModelJsonConverterTest.TestHealthCheckName,
      HttpHealthCheckModelJsonConverterTest.TestHealthCheckDescription,
      HealthCheckType.HttpRequest,
      originalDefinition = new HttpHealthCheckDefinition(
        HttpHealthCheckModelJsonConverterTest.TestUrl,
        conditions,
        HttpHealthCheckModelJsonConverterTest.TestFollowRedirects,
        HttpHealthCheckModelJsonConverterTest.TestAuthHeader,
        null));

    var serializedModel = JsonSerializer.Serialize(original, HttpHealthCheckModelJsonConverterTest.DefaultOptions);

    Assert.NotNull(serializedModel);

    var deserializedModel = JsonSerializer.Deserialize<HealthCheckModel>(serializedModel, HttpHealthCheckModelJsonConverterTest.DefaultOptions);

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
          Url = HttpHealthCheckModelJsonConverterTest.TestUrl,
          FollowRedirects = HttpHealthCheckModelJsonConverterTest.TestFollowRedirects,
          AuthorizationHeader = HttpHealthCheckModelJsonConverterTest.TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded,
              ResponseTime = HttpHealthCheckModelJsonConverterTest.TestHttpResponseTime.ToString(),
              // Type property is missing
            },
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline,
              StatusCodes = HttpHealthCheckModelJsonConverterTest.TestStatusCodes.ToString(),
              // Type property is missing
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, HttpHealthCheckModelJsonConverterTest.DefaultOptions)
      );
  }

  [Fact]
  public void Deserialize_InvalidTypeValue_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = HttpHealthCheckModelJsonConverterTest.TestUrl,
          FollowRedirects = HttpHealthCheckModelJsonConverterTest.TestFollowRedirects,
          AuthorizationHeader = HttpHealthCheckModelJsonConverterTest.TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded,
              ResponseTime = HttpHealthCheckModelJsonConverterTest.TestHttpResponseTime.ToString(),
              // Invalid type
              Type = "invalid type"
            },
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline,
              StatusCodes = HttpHealthCheckModelJsonConverterTest.TestStatusCodes.ToString(),
              // Invalid type
              Type = "invalid type"
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, HttpHealthCheckModelJsonConverterTest.DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingResponseTime_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = HttpHealthCheckModelJsonConverterTest.TestUrl,
          FollowRedirects = HttpHealthCheckModelJsonConverterTest.TestFollowRedirects,
          AuthorizationHeader = HttpHealthCheckModelJsonConverterTest.TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded,
              // Missing Response Time property
              Type = HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeResponse
            },
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline,
              StatusCodes = HttpHealthCheckModelJsonConverterTest.TestStatusCodes.ToString(),
              Type = HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeStatus
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, HttpHealthCheckModelJsonConverterTest.DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingStatusCodes_JsonException() {
    var invalidObj =
      JsonSerializer.Serialize(
        new {
          Url = HttpHealthCheckModelJsonConverterTest.TestUrl,
          FollowRedirects = HttpHealthCheckModelJsonConverterTest.TestFollowRedirects,
          AuthorizationHeader = HttpHealthCheckModelJsonConverterTest.TestAuthHeader,
          Conditions = new object[] {
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusDegraded,
              ResponseTime = HttpHealthCheckModelJsonConverterTest.TestHttpResponseTime.ToString(),
              Type = HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeResponse
            },
            new {
              Status = HttpHealthCheckModelJsonConverterTest.TestHealthStatusOnline,
              // Missing Status Code property
              Type = HttpHealthCheckModelJsonConverterTest.TestHttpConditionTypeStatus
            },
          }
        });

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HttpHealthCheckCondition>(invalidObj, HttpHealthCheckModelJsonConverterTest.DefaultOptions)
    );
  }
};




