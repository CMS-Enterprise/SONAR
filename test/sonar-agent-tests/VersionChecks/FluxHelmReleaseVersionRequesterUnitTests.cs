using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.VersionChecks;
using Cms.BatCave.Sonar.Models;
using Json.More;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Agent.Tests.VersionChecks;

public class FluxHelmReleaseVersionRequesterUnitTests {
  private readonly ITestOutputHelper _output;
  private readonly String _pathToFileWithVersionInfo;
  private readonly FluxHelmReleaseVersionCheckDefinition _matchingDefinition;

  public FluxHelmReleaseVersionRequesterUnitTests(ITestOutputHelper output) {
    this._output = output;

    this._pathToFileWithVersionInfo = ".../../../../../test-inputs/helmRelease-with-version.json";

    this._matchingDefinition = new FluxHelmReleaseVersionCheckDefinition(
      k8sNamespace: "sample-helmrelease",
      helmRelease: "sample-helmrelease"
    );
  }

  [Fact]
  public async Task GetFluxHelmReleaseVersion_NothingConfigured_ThrowsVersionRequestException() {
    // Mock up an empty kubeClient with no configuration
    var mockKubeClient = new Mock<IKubernetes>();
    var requester = new FluxHelmReleaseVersionRequester(mockKubeClient.Object, Mock.Of<ILogger<FluxHelmReleaseVersionRequester>>());

    // Assert an VersionRequestException is  thrown
    await Assert.ThrowsAsync<VersionRequestException>(() =>
      requester.GetVersionAsync(this._matchingDefinition));
  }

  [Theory]
  [InlineData("sample-namespace", "invalidNS", "sample-helmrelease")]
  [InlineData("sample-namespace", "sample-namespace", "invalidHR")]
  public async Task GetFluxHelmReleaseVersion_NotFound_ThrowsVersionRequestException(
    String k8sNamespace,
    String targetNamespace,
    String targetHelmRelease) {
    await using var jsonStream = new FileStream(this._pathToFileWithVersionInfo, FileMode.Open, FileAccess.Read);
    String jsonContents;
    using (StreamReader reader = new StreamReader(jsonStream)) {
      jsonContents = reader.ReadToEnd();
    }

    var listNamespacedCustomObjectsResponse = new HttpOperationResponse<Object>();
    JsonDocument jsonDoc = JsonDocument.Parse(jsonContents);
    listNamespacedCustomObjectsResponse.Body = jsonDoc.RootElement;

    var mockKubeClient = new Mock<IKubernetes>();
    mockKubeClient
      .Setup(client =>
        client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
          It.IsAny<String>(), It.IsAny<String>(), k8sNamespace, It.IsAny<String>(),
          null, null, null, null, null, null, null, null, null, null, null,
          default)
      )
      .ReturnsAsync(listNamespacedCustomObjectsResponse);

    var differentDefinition = new FluxHelmReleaseVersionCheckDefinition(
      k8sNamespace: targetNamespace,
      helmRelease: targetHelmRelease
    );

    var requester = new FluxHelmReleaseVersionRequester(mockKubeClient.Object, Mock.Of<ILogger<FluxHelmReleaseVersionRequester>>());

    await Assert.ThrowsAsync<VersionRequestException>(() =>
      requester.GetVersionAsync(differentDefinition));
  }

  [Fact]
  public async Task GetFluxHelmReleaseVersion_HelmReleaseInNamespace_VersionUnknown() {
    await using var jsonStream = new FileStream(".../../../../../test-inputs/helmRelease-without-version.json", FileMode.Open, FileAccess.Read);
    String jsonContents;
    using (StreamReader reader = new StreamReader(jsonStream)) {
      jsonContents = reader.ReadToEnd();
    }

    var listNamespacedCustomObjectsResponse = new HttpOperationResponse<Object>();
    JsonDocument jsonDoc = JsonDocument.Parse(jsonContents);
    listNamespacedCustomObjectsResponse.Body = jsonDoc.RootElement;

    var mockKubeClient = new Mock<IKubernetes>();
    mockKubeClient
      .Setup(client =>
        client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
          It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>(), null,
          null, null, null, null, null, null, null, null, null, null,
          default)
      )
      .ReturnsAsync(listNamespacedCustomObjectsResponse);

    var requester = new FluxHelmReleaseVersionRequester(mockKubeClient.Object, Mock.Of<ILogger<FluxHelmReleaseVersionRequester>>());
    var response = await requester.GetVersionAsync(this._matchingDefinition);

    var expectedVersion = "Unknown";
    Assert.Equal(expectedVersion, response.Version);
  }

  [Fact]
  public async Task GetFluxHelmReleaseVersion_HelmReleaseInNamespace_SuccessfulVersionExtraction() {
    await using var jsonStream = new FileStream(this._pathToFileWithVersionInfo, FileMode.Open, FileAccess.Read);
    String jsonContents;
    using (StreamReader reader = new StreamReader(jsonStream)) {
      jsonContents = reader.ReadToEnd();
    }

    var listNamespacedCustomObjectsResponse = new HttpOperationResponse<Object>();
    JsonDocument jsonDoc = JsonDocument.Parse(jsonContents);
    listNamespacedCustomObjectsResponse.Body = jsonDoc.RootElement;

    var mockKubeClient = new Mock<IKubernetes>();
    mockKubeClient
      .Setup(client =>
        client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
          It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>(), It.IsAny<String>(),
          null, null, null, null, null, null, null, null, null, null, null,
          default)
      )
      .ReturnsAsync(listNamespacedCustomObjectsResponse);

    var requester = new FluxHelmReleaseVersionRequester(mockKubeClient.Object, Mock.Of<ILogger<FluxHelmReleaseVersionRequester>>());
    var response = await requester.GetVersionAsync(this._matchingDefinition);

    Assert.Equal("6.5.3", response.Version);
  }
}
