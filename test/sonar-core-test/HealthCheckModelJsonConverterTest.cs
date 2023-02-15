using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Xunit;

namespace Cms.BatCave.Sonar.SonarCoreTest;

public class HealthCheckModelJsonConverterTest {
  private static readonly JsonSerializerOptions DefaultOptions = new() {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  // Successful Deserialization: Prometheus
  // Successful Serialization: Prometheus
  // Error: Name missing
  // Error: Description Missing
  // Error: Type Missing
  // Error: Invalid Type
  // Error: Definition Missing
  // Error: Definition with Missing Expression
  // Error: Definition with Missing Conditions

  private const String TestHealthCheckName = "Example";
  private const String TestHealthCheckDescription = "Test Description";
  private static readonly TimeSpan TestPrometheusHealthCheckDuration = TimeSpan.Parse("1.23:00:01");
  private const String TestPrometheusHealthCheckExpression = "test_metric";
  private const HealthOperator TestPrometheusHealthCheckOperator = HealthOperator.GreaterThan;
  private const Decimal TestPrometheusHealthCheckThreshold = 3.14m;
  private const HealthStatus TestPrometheusHealthCheckStatus = HealthStatus.Degraded;

  private static readonly String ValidPrometheusHealthCheck =
    JsonSerializer.Serialize(
      new {
        name = TestHealthCheckName,
        description = TestHealthCheckDescription,
        type = "PrometheusMetric",
        definition = new {
          duration = TestPrometheusHealthCheckDuration.ToString(),
          expression = TestPrometheusHealthCheckExpression,
          conditions = new[] {
            new {
              @operator = TestPrometheusHealthCheckOperator.ToString(),
              threshold = TestPrometheusHealthCheckThreshold,
              status = TestPrometheusHealthCheckStatus.ToString()
            }
          }
        }
      }
    );

  [Fact]
  public void Deserialize_PrometheusHealthCheck_Success() {
    var result =
      JsonSerializer.Deserialize<HealthCheckModel>(ValidPrometheusHealthCheck, DefaultOptions);

    Assert.NotNull(result);
    Assert.Equal(TestHealthCheckName, result.Name);
    Assert.Equal(TestHealthCheckDescription, result.Description);
    Assert.Equal(HealthCheckType.PrometheusMetric, result.Type);
    var definition = Assert.IsType<PrometheusHealthCheckDefinition>(result.Definition);
    Assert.Equal(TestPrometheusHealthCheckDuration, definition.Duration);
    Assert.Equal(TestPrometheusHealthCheckExpression, definition.Expression);
    var condition = Assert.Single(definition.Conditions);
    Assert.Equal(TestPrometheusHealthCheckOperator, condition.Operator);
    Assert.Equal(TestPrometheusHealthCheckThreshold, condition.Threshold);
    Assert.Equal(TestPrometheusHealthCheckStatus, condition.Status);
  }

  [Fact]
  public void SerializeRoundTrip_PrometheusHealthCheck_Success() {
    PrometheusHealthCheckDefinition originalDefinition;
    var original = new HealthCheckModel(
      TestHealthCheckName,
      TestHealthCheckDescription,
      HealthCheckType.PrometheusMetric,
      originalDefinition = new PrometheusHealthCheckDefinition(
        TestPrometheusHealthCheckDuration,
        TestPrometheusHealthCheckExpression,
        ImmutableList.Create(
          new MetricHealthCondition(
            TestPrometheusHealthCheckOperator,
            TestPrometheusHealthCheckThreshold,
            TestPrometheusHealthCheckStatus
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
    var deserializedDefinition = Assert.IsType<PrometheusHealthCheckDefinition>(deserializedModel.Definition);
    Assert.Equal(originalDefinition.Duration, deserializedDefinition.Duration);
    Assert.Equal(originalDefinition.Expression, deserializedDefinition.Expression);
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
          type = "PrometheusMetric",
          definition = new {
            duration = TestPrometheusHealthCheckDuration.ToString(),
            expression = TestPrometheusHealthCheckExpression,
            conditions = new[] {
              new {
                @operator = TestPrometheusHealthCheckOperator.ToString(),
                threshold = TestPrometheusHealthCheckThreshold,
                status = TestPrometheusHealthCheckStatus.ToString()
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
          type = "PrometheusMetric",
          definition = new {
            duration = TestPrometheusHealthCheckDuration.ToString(),
            expression = TestPrometheusHealthCheckExpression,
            conditions = new[] {
              new {
                @operator = TestPrometheusHealthCheckOperator.ToString(),
                threshold = TestPrometheusHealthCheckThreshold,
                status = TestPrometheusHealthCheckStatus.ToString()
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
            duration = TestPrometheusHealthCheckDuration.ToString(),
            expression = TestPrometheusHealthCheckExpression,
            conditions = new[] {
              new {
                @operator = TestPrometheusHealthCheckOperator.ToString(),
                threshold = TestPrometheusHealthCheckThreshold,
                status = TestPrometheusHealthCheckStatus.ToString()
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
            duration = TestPrometheusHealthCheckDuration.ToString(),
            expression = TestPrometheusHealthCheckExpression,
            conditions = new[] {
              new {
                @operator = TestPrometheusHealthCheckOperator.ToString(),
                threshold = TestPrometheusHealthCheckThreshold,
                status = TestPrometheusHealthCheckStatus.ToString()
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
          type = "PrometheusMetric"
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
          type = "PrometheusMetric",
          definition = new {
            duration = TestPrometheusHealthCheckDuration.ToString(),
            // Missing expression
            conditions = new[] {
              new {
                @operator = TestPrometheusHealthCheckOperator.ToString(),
                threshold = TestPrometheusHealthCheckThreshold,
                status = TestPrometheusHealthCheckStatus.ToString()
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
          type = "PrometheusMetric",
          definition = new {
            duration = TestPrometheusHealthCheckDuration.ToString(),
            expression = TestPrometheusHealthCheckExpression,
            // Missing conditions
          }
        }
      );

    Assert.Throws<JsonException>(
      () => JsonSerializer.Deserialize<HealthCheckModel>(invalidJson, DefaultOptions)
    );
  }
}
