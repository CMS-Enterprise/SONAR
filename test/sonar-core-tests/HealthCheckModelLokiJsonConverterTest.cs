using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Loki;
using Cms.BatCave.Sonar.Models;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class HealthCheckModelLokiJsonConverterTest {
  private static readonly JsonSerializerOptions DefaultOptions = new() {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  // Successful Deserialization: Loki
  // Successful Serialization: Loki
  // Error: Name missing
  // Error: Description Missing
  // Error: Type Missing
  // Error: Invalid Type
  // Error: Definition Missing
  // Error: Definition with Missing Expression
  // Error: Definition with Missing Conditions

  private const String TestHealthCheckName = "LokiExample";
  private const String TestHealthCheckDescription = "Test Description";
  private static readonly TimeSpan TestLokiDuration = TimeSpan.Parse("1.23:00:01");
  private const String TestLokiHealthCheckExpression = "test_metric";
  private const HealthOperator TestLokiHealthCheckOperator = HealthOperator.GreaterThan;
  private const Decimal TestLokiHealthCheckThreshold = 3.23m;
  private const HealthStatus TestLokiHealthCheckStatus = HealthStatus.Degraded;

  private const Int32 TestLokiLogLimit = 12;
  private static DateTime TestHealthCheckTime = DateTime.UtcNow;
  private const Direction TestLokiDirection = Direction.Backward;

  private static readonly String ValidLokiHealthCheck =
    JsonSerializer.Serialize(
      new {
        name = TestHealthCheckName,
        description = TestHealthCheckDescription,
        type = "LokiMetric",
        definition = new {
          expression = TestLokiHealthCheckExpression,
          duration = TestLokiDuration,
          conditions = new[] {
            new {
              @operator = TestLokiHealthCheckOperator.ToString(),
              threshold = TestLokiHealthCheckThreshold,
              status = TestLokiHealthCheckStatus.ToString()
            }
          }
        }
      }
    );

  [Fact]
  public void Deserialize_LokiHealthCheck_Success() {
    var result =
      JsonSerializer.Deserialize<HealthCheckModel>(ValidLokiHealthCheck, DefaultOptions);

    Assert.NotNull(result);
    Assert.Equal(TestHealthCheckName, result.Name);
    Assert.Equal(TestHealthCheckDescription, result.Description);
    Assert.Equal(HealthCheckType.LokiMetric, result.Type);
    var definition = Assert.IsType<LokiHealthCheckDefinition>(result.Definition);
    Assert.Equal(TestLokiHealthCheckExpression, definition.Expression);
    Assert.Equal(TestLokiDuration, definition.Duration);
    var condition = Assert.Single(definition.Conditions);
    Assert.Equal(TestLokiHealthCheckOperator, condition.Operator);
    Assert.Equal(TestLokiHealthCheckThreshold, condition.Threshold);
    Assert.Equal(TestLokiHealthCheckStatus, condition.Status);
  }

  [Fact]
  public void SerializeRoundTrip_LokiHealthCheck_Success() {
    LokiHealthCheckDefinition originalDefinition;
    var original = new HealthCheckModel(
      TestHealthCheckName,
      TestHealthCheckDescription,
      HealthCheckType.LokiMetric,
      originalDefinition = new LokiHealthCheckDefinition(
        TestLokiDuration,
        TestLokiHealthCheckExpression,
        ImmutableList.Create(
          new MetricHealthCondition(
            TestLokiHealthCheckOperator,
            TestLokiHealthCheckThreshold,
            TestLokiHealthCheckStatus
          )
        )
      )
    );

    var serializedModel = JsonSerializer.Serialize(original, DefaultOptions);

    Assert.NotNull(serializedModel);

    // Round trip
    var deserializedModel = JsonSerializer.Deserialize<HealthCheckModel>(serializedModel, DefaultOptions);

    Assert.NotNull(deserializedModel);
    Assert.Equal(original.Name, deserializedModel.Name);
    Assert.Equal(original.Description, deserializedModel.Description);
    Assert.Equal(original.Type, deserializedModel.Type);
    var deserializedDefinition = Assert.IsType<LokiHealthCheckDefinition>(deserializedModel.Definition);
    Assert.Equal(originalDefinition.Expression, deserializedDefinition.Expression);
    Assert.Equal(originalDefinition.Duration, deserializedDefinition.Duration);
    var condition = Assert.Single(deserializedDefinition.Conditions);
    Assert.Equal(originalDefinition.Conditions[0].Operator, condition.Operator);
    Assert.Equal(originalDefinition.Conditions[0].Threshold, condition.Threshold);
    Assert.Equal(originalDefinition.Conditions[0].Status, condition.Status);
  }

  [Fact]
  public void Deserialize_MissingName_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          // Name is not specified
          description = TestHealthCheckDescription,
          type = "LokiMetric",
          definition = new {
            expression = TestLokiHealthCheckExpression,
            time = TestHealthCheckTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            conditions = new[] {
              new {
                @operator = TestLokiHealthCheckOperator.ToString(),
                threshold = TestLokiHealthCheckThreshold,
                status = TestLokiHealthCheckStatus.ToString()
              }
            }
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingDescription_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          name = TestHealthCheckName,
          // Missing Description
          type = "LokiMetric",
          definition = new {
            expression = TestLokiHealthCheckExpression,
            time = TestHealthCheckTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            conditions = new[] {
              new {
                @operator = TestLokiHealthCheckOperator.ToString(),
                threshold = TestLokiHealthCheckThreshold,
                status = TestLokiHealthCheckStatus.ToString()
              }
            }
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingType_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          name = TestHealthCheckName,
          description = TestHealthCheckDescription,
          // Missing type
          definition = new {
            expression = TestLokiHealthCheckExpression,
            time = TestHealthCheckTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            conditions = new[] {
              new {
                @operator = TestLokiHealthCheckOperator.ToString(),
                threshold = TestLokiHealthCheckThreshold,
                status = TestLokiHealthCheckStatus.ToString()
              }
            }
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_InvalidType_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          name = TestHealthCheckName,
          description = TestHealthCheckDescription,
          type = "InvalidValue",
          definition = new {
            expression = TestLokiHealthCheckExpression,
            time = TestHealthCheckTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            conditions = new[] {
              new {
                @operator = TestLokiHealthCheckOperator.ToString(),
                threshold = TestLokiHealthCheckThreshold,
                status = TestLokiHealthCheckStatus.ToString()
              }
            }
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_MissingDefinition_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          name = TestHealthCheckName,
          description = TestHealthCheckDescription,
          type = "LokiMetric"
          // Missing definition
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_DefinitionMissingExpression_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          name = TestHealthCheckName,
          description = TestHealthCheckDescription,
          type = "LokiMetric",
          definition = new {
            time = TestHealthCheckTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            // Missing expression
            conditions = new[] {
              new {
                @operator = TestLokiHealthCheckOperator.ToString(),
                threshold = TestLokiHealthCheckThreshold,
                status = TestLokiHealthCheckStatus.ToString()
              }
            }
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }

  [Fact]
  public void Deserialize_DefinitionMissingConditions_JsonException() {
    var invalidJson =
      JsonSerializer.Serialize(
        new {
          name = TestHealthCheckName,
          description = TestHealthCheckDescription,
          type = "LokiMetric",
          definition = new {
            expression = TestLokiHealthCheckExpression,
            time = TestHealthCheckTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            // Missing conditions
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }
}
