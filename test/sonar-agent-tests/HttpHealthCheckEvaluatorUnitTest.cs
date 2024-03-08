using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Agent.HealthChecks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests;

public class HttpHealthCheckEvaluatorUnitTest {

  private readonly Mock<ILogger<HttpHealthCheckEvaluator>> _mockLogger = new();
  private readonly Mock<IOptions<AgentConfiguration>> _testAgentConfig = new();

  private const String TestUriString = "http://localhost:8082/api/ready";

  private readonly HealthCheckIdentifier _testHealthCheckIdentifier = new(
    "env", "ten", "svc", "example");

  private readonly HttpHealthCheckDefinition _basicTestHttpHealthCheckDef = new(
    url: new Uri(TestUriString),
    conditions: new HttpHealthCheckCondition[] {
      new StatusCodeCondition(new UInt16[] { 200 }, HealthStatus.Online)
    }
  );

  public static readonly IEnumerable<Object[]> HandledOfflineErrorTypes = new List<Object[]> {
    new Object[] {
      new HttpRequestException("network error")
    },
    new Object[] {
      new OperationCanceledException("task canceled")
    }
  };

  public static readonly IEnumerable<Object[]> HandledUnknownErrorTypes = new List<Object[]> {
    new Object[] {
      new InvalidOperationException("invalid operation")
    },
    new Object[] {
      new UriFormatException("error with requestURI")
    }
  };

  public HttpHealthCheckEvaluatorUnitTest() {
    this._testAgentConfig
      .SetupGet(options => options.Value)
      .Returns(new AgentConfiguration(
        DefaultTenant: "ten",
        MaximumConcurrency: 1)
      );
  }

  [Fact]
  public async Task HttpHealthCheckEvaluator_ResponseTimeStatusMoreSevereThanStatusCode() {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;

    var testHttpHealthCheckDef = new HttpHealthCheckDefinition(
      url: new Uri(TestUriString),
      conditions: new HttpHealthCheckCondition[] {
        new StatusCodeCondition(new UInt16[] { 503 }, HealthStatus.AtRisk),
        new ResponseTimeCondition( new TimeSpan(0, 0, 0, 0), HealthStatus.Degraded)
      },
      followRedirects: true,
      authorizationHeader: "test-header"
    );

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable });
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef
    );

    Assert.Equal(HealthStatus.Degraded, result);
  }

  [Fact]
  public async Task HttpHealthCheckEvaluator_BothXmlAndJsonInResponseBody() {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;

    var testHttpHealthCheckDef = new HttpHealthCheckDefinition(
      url: new Uri(TestUriString),
      conditions: new HttpHealthCheckCondition[] {
        new HttpBodyHealthCheckCondition(
          HealthStatus.Online,
          HttpHealthCheckConditionType.HttpBodyJson,
          path: "foo",
          value: "bar"
          ),
        new HttpBodyHealthCheckCondition(
          HealthStatus.Online,
          HttpHealthCheckConditionType.HttpBodyXml,
          path: "foo",
          value: "bar"
        )
      }
    );

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef
    );

    Assert.Equal(HealthStatus.Unknown, result);
  }

  [Fact]
  public async Task HttpHealthCheckEvaluator_TaskCanceled() {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;
    var testHttpHealthCheckDef = this._basicTestHttpHealthCheckDef;

    var cts = new CancellationTokenSource();

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback(new Action<HttpRequestMessage, CancellationToken>((_, cancellationToken) => {
        Thread.Sleep(20);
      }))
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    cts.CancelAfter(TimeSpan.FromMilliseconds(1));

    await Assert.ThrowsAsync<TaskCanceledException>(async () =>
      await evaluator.EvaluateHealthCheckAsync(
        testHealthCheckIdentifier,
        testHttpHealthCheckDef,
        cts.Token
      ));
  }

  [Theory]
  [MemberData(nameof(HandledUnknownErrorTypes))]
  public async Task HttpHealthCheckEvaluator_ExceptionsResultingInUnknown(Exception ex) {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;
    var testHttpHealthCheckDef = this._basicTestHttpHealthCheckDef;

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ThrowsAsync(ex);
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef
    );

    Assert.Equal(HealthStatus.Unknown, result);
  }

  [Theory]
  [MemberData(nameof(HandledOfflineErrorTypes))]
  public async Task HttpHealthCheckEvaluator_ExceptionsResultingInOffline(Exception ex) {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;
    var testHttpHealthCheckDef = this._basicTestHttpHealthCheckDef;

    var cts = new CancellationTokenSource();

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ThrowsAsync(ex);
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef,
      cts.Token
    );

    Assert.Equal(HealthStatus.Offline, result);
  }

  [Theory]
  [InlineData(true, "test-header", HealthStatus.Online, HttpStatusCode.OK)]
  [InlineData(true, null, HealthStatus.Online, HttpStatusCode.OK)]
  [InlineData(false, "some/path; someVar=", HealthStatus.Offline, HttpStatusCode.GatewayTimeout)]
  [InlineData(false, null, HealthStatus.Offline, HttpStatusCode.GatewayTimeout)]
  public async Task HttpHealthCheckEvaluator_NoXmlNorJsonInResponseBody_ReturnExpectedHealthStatus(
    Boolean followRedirectsVal,
    String? authHeaderVal,
    HealthStatus expectedStatus,
    HttpStatusCode statusCode) {
    var mockAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;

    var testHttpHealthCheckDef = new HttpHealthCheckDefinition(
      url: new Uri(TestUriString),
      conditions: new HttpHealthCheckCondition[] {
        new StatusCodeCondition(new UInt16[] { 200 }, HealthStatus.Online),
        new ResponseTimeCondition( new TimeSpan(0, 0, 0, 2), HealthStatus.Degraded)
      },
      followRedirects: followRedirectsVal,
      authorizationHeader: authHeaderVal
    );

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      mockAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef
    );

    Assert.Equal(expectedStatus, result);
  }

  [Theory]
  [InlineData(HttpHealthCheckConditionType.HttpBodyJson)]
  [InlineData(HttpHealthCheckConditionType.HttpBodyXml)]
  public async Task HttpHealthCheckEvaluator_EvaluateCondition_DocumentValueExtractorException(
    HttpHealthCheckConditionType httpBodyType) {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;

    var testHttpHealthCheckDef = new HttpHealthCheckDefinition(
      url: new Uri(TestUriString),
      conditions: new HttpHealthCheckCondition[] {
        new HttpBodyHealthCheckCondition(
          HealthStatus.Online,
          httpBodyType,
          "foo",
          "bar")
      }
    );

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef
    );

    Assert.Equal(HealthStatus.Unknown, result);
  }

  [Theory]
  [InlineData(HealthStatus.Offline, "1.0", HealthStatus.Offline)]
  [InlineData(HealthStatus.Offline, "10", HealthStatus.Unknown)]
  public async Task HttpHealthCheckEvaluator_EvaluateCondition_ValidDocumentValue(
    HealthStatus inputHealthStatus,
    String documentValue,
    HealthStatus expectedHealthStatus) {
    var testAgentConfig = this._testAgentConfig;
    var testHealthCheckIdentifier = this._testHealthCheckIdentifier;
    var testHttpBodyHealthCheckCondition = new HttpBodyHealthCheckCondition(
      inputHealthStatus,
      HttpHealthCheckConditionType.HttpBodyJson,
      "$.version",
      documentValue);

    var testHttpHealthCheckDef = new HttpHealthCheckDefinition(
      url: new Uri(TestUriString),
      conditions: new HttpHealthCheckCondition[] {
        new StatusCodeCondition(new UInt16[] { 503 }, HealthStatus.AtRisk),
        testHttpBodyHealthCheckCondition
      }
    );

    // Set up SendAsync method behavior
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    mockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(
        new HttpResponseMessage {
          StatusCode = HttpStatusCode.ServiceUnavailable,
          Content = JsonContent.Create(new { version = "1.0" })
        });
    mockHttpMessageHandler
      .Protected()
      .Setup(
        "Dispose",
        ItExpr.IsAny<Boolean>());

    var evaluator = new HttpHealthCheckEvaluator(
      testAgentConfig.Object,
      this._mockLogger.Object,
      testHttpHealthCheckDef => mockHttpMessageHandler.Object,
      () => (Mock.Of<IDisposable>(), Mock.Of<ISonarClient>())
    );

    var result = await evaluator.EvaluateHealthCheckAsync(
      testHealthCheckIdentifier,
      testHttpHealthCheckDef
    );

    Assert.Equal(expectedHealthStatus, result);
  }
}
