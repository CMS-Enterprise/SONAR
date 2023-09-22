using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.VersionChecks;
using Cms.BatCave.Sonar.Models;
using k8s;
using k8s.Autorest;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Agent.Tests.VersionChecks;

public class FluxKustomizationVersionRequesterUnitTests {
  private readonly ITestOutputHelper _output;
  private readonly String _pathToFileWithVersionInfo;
  private readonly FluxKustomizationVersionCheckDefinition _matchingDefinition;

  public FluxKustomizationVersionRequesterUnitTests(ITestOutputHelper output) {
    this._output = output;

    this._pathToFileWithVersionInfo = ".../../../../../test-inputs/kustomization-with-version-info.json";

    this._matchingDefinition = new FluxKustomizationVersionCheckDefinition(
      k8sNamespace: "sample-kustomization",
      kustomization: "sample-kustomization"
    );
  }

  [Fact]
  public async Task GetFluxKustomizationVersion_NothingConfigured_ThrowsVersionRequestException() {
    var mockKubeClient = new Mock<IKubernetes>();
    var requester = new FluxKustomizationVersionRequester(mockKubeClient.Object);

    var exception = await Assert.ThrowsAnyAsync<VersionRequestException>(() =>
      requester.GetVersionAsync(this._matchingDefinition));

    Assert.Equal("Version request failed.", exception.Message);
  }

  [Fact]
  public async Task GetFluxKustomizationVersion_KustomizationNotInNamespace_ThrowsVersionRequestException() {
    await using var jsonStream = new FileStream(this._pathToFileWithVersionInfo, FileMode.Open, FileAccess.Read);
    String jsonContents;
    using (StreamReader reader = new StreamReader(jsonStream)) {
      jsonContents = reader.ReadToEnd();
    }

    var listNamespacedCustomObjectsResponse = new HttpOperationResponse<object>();
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

    var differentDefinition = new FluxKustomizationVersionCheckDefinition(
      k8sNamespace: "sample",
      kustomization: "sample"
    );

    var requester = new FluxKustomizationVersionRequester(mockKubeClient.Object);
    var exception = await Assert.ThrowsAnyAsync<VersionRequestException>(() =>
      requester.GetVersionAsync(differentDefinition));

    Assert.Equal($"Kustomization {differentDefinition.Kustomization} not found in {differentDefinition.K8sNamespace}",
      exception.Message);
  }

  [Fact]
  public async Task GetFluxKustomizationVersion_KustomizationInNamespace_VersionUnknown() {
    await using var jsonStream = new FileStream("../../../test-inputs/kustomization-without-version-info.json", FileMode.Open, FileAccess.Read);
    String jsonContents;
    using (StreamReader reader = new StreamReader(jsonStream)) {
      jsonContents = reader.ReadToEnd();
    }

    var listNamespacedCustomObjectsResponse = new HttpOperationResponse<object>();
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

    var differentDefinition = new FluxKustomizationVersionCheckDefinition(
      k8sNamespace: "sample",
      kustomization: "sample"
    );

    var requester = new FluxKustomizationVersionRequester(mockKubeClient.Object);
    var response = await requester.GetVersionAsync(this._matchingDefinition);

    var expectedVersion = "Unknown";
    Assert.Equal(expectedVersion, response.Version);
  }

  [Fact]
  public async Task GetFluxKustomizationVersion_KustomizationInNamespace_SuccessfulVersionExtraction() {
    await using var jsonStream = new FileStream(this._pathToFileWithVersionInfo, FileMode.Open, FileAccess.Read);
    String jsonContents;
    using (StreamReader reader = new StreamReader(jsonStream)) {
      jsonContents = reader.ReadToEnd();
    }

    var listNamespacedCustomObjectsResponse = new HttpOperationResponse<object>();
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

    var requester = new FluxKustomizationVersionRequester(mockKubeClient.Object);
    var response = await requester.GetVersionAsync(this._matchingDefinition);

    var expectedVersion = "feature-branch-name@commit-hash";
    Assert.Equal(expectedVersion, response.Version);
  }
}
