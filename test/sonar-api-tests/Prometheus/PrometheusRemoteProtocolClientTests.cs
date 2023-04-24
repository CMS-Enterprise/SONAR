using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Prometheus;
using Google.Protobuf;
using Moq;
using Moq.Protected;
using Prometheus;
using Snappy;
using Xunit;

namespace Cms.BatCave.Sonar.Tests.Prometheus;

public class PrometheusRemoteProtocolClientTests {

  private readonly PrometheusRemoteProtocolClient _prometheusRemoteProtocolClient;

  /// <summary>
  /// <see cref="PrometheusRemoteProtocolClient"/> depends on HttpClient, which we'd like to mock in these tests.
  /// Moq can't mock <see cref="HttpClient"/> directly, so instead we mock the abstract <see cref="HttpMessageHandler"/>
  /// that HttpClient uses under the hood. The methods in HttpMessageHandler are <c>protected internal</c>, so we must
  /// use Moq's protected setups feature to set up behavior for this mock. See "Setting expectations for protected
  /// members..." at <a href="https://github.com/Moq/moq4/wiki/Quickstart#miscellaneous">Moq Quickstart/Miscellaneous</a>.
  /// </summary>
  private Mock<HttpMessageHandler> MockHttpMessageHandler { get; } = new();

  public PrometheusRemoteProtocolClientTests() {
    var httpClient = new HttpClient(this.MockHttpMessageHandler.Object);
    httpClient.BaseAddress = new Uri("http://localhost");
    this._prometheusRemoteProtocolClient = new PrometheusRemoteProtocolClient(httpClient);
  }

  [Theory]
  [InlineData(HttpStatusCode.BadRequest, typeof(BadRequestException))]
  [InlineData(HttpStatusCode.NotFound, typeof(BadRequestException))]
  [InlineData(HttpStatusCode.InternalServerError, typeof(InternalServerErrorException))]
  [InlineData(HttpStatusCode.ServiceUnavailable, typeof(InternalServerErrorException))]
  public async Task WriteAsync_WhenPrometheusRequestIsNotSuccessful_AppropriateExceptionIsThrown(
    HttpStatusCode nonSuccessStatusCode,
    Type expectedExceptionType) {

    this.MockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        methodOrPropertyName: "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(new HttpResponseMessage { StatusCode = nonSuccessStatusCode })
      .Verifiable();

    await Assert.ThrowsAsync(
      expectedExceptionType,
      testCode: () => this._prometheusRemoteProtocolClient.WriteAsync(new WriteRequest()));
    this.MockHttpMessageHandler.Verify();
  }

  [Fact]
  public async Task WriteAsync_CompressesPrometheusRequestContent() {
    HttpRequestMessage? capturedHttpRequestMessage = null;

    this.MockHttpMessageHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        methodOrPropertyName: "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>(
        (httpRequestMessage, _) => capturedHttpRequestMessage = httpRequestMessage)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

    await this._prometheusRemoteProtocolClient.WriteAsync(new WriteRequest {
      Metadata = {
        new MetricMetadata {
          Help = "Test Metric Metadata"
        }
      }
    });

    if (capturedHttpRequestMessage!.Content is ByteArrayContent byteArrayContent) {
      var writeRequestBytes = SnappyCodec.Uncompress(await byteArrayContent.ReadAsByteArrayAsync());
      var parser = new MessageParser<WriteRequest>(() => new WriteRequest());
      var writeRequest = parser.ParseFrom(writeRequestBytes);
      Assert.Equal(expected: "Test Metric Metadata", writeRequest.Metadata[0].Help);
    } else {
      Assert.True(condition: false, $"Captured message content is not {typeof(ByteArrayContent)}");
    }
  }
}
