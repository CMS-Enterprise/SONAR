using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Agent.HealthChecks.Metrics;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class MetricHealthCheckEvaluatorUnitTest {
  [Theory]
  [InlineData(HealthOperator.Equal, 4, 4, 4, 4)]
  [InlineData(HealthOperator.NotEqual, 4, 5, 3, -4)]
  [InlineData(HealthOperator.GreaterThan, 4, 5, Int32.MaxValue)]
  [InlineData(HealthOperator.GreaterThanOrEqual, 4, 4, 5, Int32.MaxValue)]
  [InlineData(HealthOperator.LessThan, 4, 3, Int32.MinValue)]
  [InlineData(HealthOperator.LessThanOrEqual, 4, 3, 4, Int32.MinValue)]
  public async Task OperatorTrueForAll_ConditionMatches(
    HealthOperator @operator,
    Int32 thresholdValue,
    params Int32[] sampleValues) {

    var queryRunner = MockMetricQueryRunner.CreateFixedReturnMock(
      sampleValues.Select(v => (DateTime.Now, (Decimal)v)).ToImmutableList()
    );

    var evaluator = new MetricHealthCheckEvaluator(
      queryRunner.Object,
      Mock.Of<ILogger<MetricHealthCheckEvaluator>>()
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      new HealthCheckIdentifier("env", "ten", "svc", "example"),
      new MetricHealthCheckDefinition(
        TimeSpan.FromSeconds(10),
        "example",
        ImmutableList.Create(
          new MetricHealthCondition(@operator, thresholdValue, HealthStatus.Offline)
        )
      )
    );

    Assert.Equal(HealthStatus.Offline, result);
  }

  [Theory]
  [InlineData(HealthOperator.Equal, 4, 3, 4, 4)]
  [InlineData(HealthOperator.Equal, 4, 4, 5, 4)]
  [InlineData(HealthOperator.Equal, 4, 4, 4, -4)]
  [InlineData(HealthOperator.NotEqual, 4, 5, 4, -4)]
  [InlineData(HealthOperator.GreaterThan, 4, 5, 4, Int32.MaxValue)]
  [InlineData(HealthOperator.GreaterThanOrEqual, 4, 4, 3, Int32.MaxValue)]
  [InlineData(HealthOperator.LessThan, 4, 3, 4, Int32.MinValue)]
  [InlineData(HealthOperator.LessThanOrEqual, 4, 3, 5, Int32.MinValue)]
  public async Task OperatorFalseForAny_ConditionDoesNotMatch(
    HealthOperator @operator,
    Int32 thresholdValue,
    params Int32[] sampleValues) {

    var queryRunner = MockMetricQueryRunner.CreateFixedReturnMock(
      sampleValues.Select(v => (DateTime.Now, (Decimal)v)).ToImmutableList()
    );

    var evaluator = new MetricHealthCheckEvaluator(
      queryRunner.Object,
      Mock.Of<ILogger<MetricHealthCheckEvaluator>>()
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      new HealthCheckIdentifier("env", "ten", "svc", "example"),
      new MetricHealthCheckDefinition(
        TimeSpan.FromSeconds(10),
        "example",
        ImmutableList.Create(
          new MetricHealthCondition(@operator, thresholdValue, HealthStatus.Offline)
        )
      )
    );

    Assert.Equal(HealthStatus.Online, result);
  }
}
