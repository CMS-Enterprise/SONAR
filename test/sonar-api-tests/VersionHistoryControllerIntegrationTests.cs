using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class VersionHistoryControllerIntegrationTests : ApiControllerTestsBase {
  private const String TestRootServiceName = "TestRootService";
  private const String TestRootServiceDisplayName = "Root Service Name";
  private const String TestChildServiceName = "TestChildService";
  private const String TestChildServiceDisplayName = "Child Service Name";
  private const String TestFluxHelmReleaseVersion = "v0.0.1";
  private const String TestFluxKustomizationVersion = "some-ref@some-commit-hash";
  private const String TestHttpResponseVersion = "v1.2.3";

  private static readonly VersionCheckModel TestFluxHelmReleaseVersionCheck =
    new(
      VersionCheckType.FluxHelmRelease,
      new FluxHelmReleaseVersionCheckDefinition(k8sNamespace: "testHelmRelease", helmRelease: "testHelmRelease")
    );

  private static readonly VersionCheckModel TestFluxKustomizationVersionCheck =
    new(
      VersionCheckType.FluxKustomization,
      new FluxKustomizationVersionCheckDefinition(k8sNamespace: "testKustomization", kustomization: "testKustomization")
    );

  private static readonly VersionCheckModel TestHttpResponseVersionCheck =
    new(
      VersionCheckType.HttpResponseBody,
      new HttpResponseBodyVersionCheckDefinition(
        url: "http://localhost:8081",
        path: "$.version",
        bodyType: HttpBodyType.Json)
    );

  private static readonly ServiceHierarchyConfiguration TestRootOnlyConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        name: TestRootServiceName,
        displayName: TestRootServiceDisplayName,
        description: null,
        url: null,
        healthChecks: null,
        versionChecks: ImmutableList.Create(TestFluxHelmReleaseVersionCheck, TestFluxKustomizationVersionCheck),
        children: null)
      ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
    );

  private static readonly ServiceHierarchyConfiguration TestRootChildConfiguration = new(
    ImmutableList.Create(
      new ServiceConfiguration(
        name: TestRootServiceName,
        displayName: TestRootServiceDisplayName,
        description: null,
        url: null,
        healthChecks: null,
        versionChecks: ImmutableList.Create(
          TestFluxKustomizationVersionCheck,
          TestFluxHelmReleaseVersionCheck),
        children: ImmutableHashSet<String>.Empty.Add(TestChildServiceName)),
      new ServiceConfiguration(
        name: TestChildServiceName,
        displayName: TestChildServiceDisplayName,
        description: null,
        url: null,
        healthChecks: null,
        versionChecks: ImmutableList.Create(TestHttpResponseVersionCheck),
        children: null)
      ),
    ImmutableHashSet<String>.Empty.Add(TestRootServiceName),
    null
    );

  public VersionHistoryControllerIntegrationTests(ApiIntegrationTestFixture fixture, ITestOutputHelper outputHelper) :
    base(fixture, outputHelper) { }

  #region Version History for Specific Service

  [Theory]
  [InlineData($"FalseEnvironment/tenants/{{testTenant}}/services/{TestRootServiceName}")]
  [InlineData($"{{testEnvironment}}/tenants/FalseTenant/services/{TestRootServiceName}")]
  [InlineData($"{{testEnvironment}}/tenants/{{testTenant}}/services/FalseService")]
  public async Task GetServiceVersionHistory_NotFound(
    String incorrectPathToService) {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    // Record service version for service
    var servicePath = $"{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}";
    var versionToRecord = new ServiceVersion(
      DateTime.UtcNow,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.FluxHelmRelease, TestFluxHelmReleaseVersion));

    await this.RecordServiceVersion(servicePath, versionToRecord);

    // Attempt to get service version history
    var getResponse = await this.GetVersionHistoryResponse(incorrectPathToService);

    Assert.Equal(
      expected: HttpStatusCode.NotFound,
      actual: getResponse.StatusCode);
  }

  [Fact]
  public async Task GetServiceVersionHistory_BadRequestException() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    // Record service version for service
    var servicePath = $"{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}";
    var versionToRecord = new ServiceVersion(
      DateTime.UtcNow,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.FluxHelmRelease, TestFluxHelmReleaseVersion));

    await this.RecordServiceVersion(servicePath, versionToRecord);

    // Attempt to get service version history
    var invalidTimestamp = DateTime.Now;
    var versionHistoryUrlPath = $"{servicePath}?timeQuery={invalidTimestamp}";
    var getResponse = await this.GetVersionHistoryResponse(versionHistoryUrlPath);

    Assert.Equal(
      expected: HttpStatusCode.BadRequest,
      actual: getResponse.StatusCode);
  }

  [Fact]
  public async Task GetServiceVersionHistory_NoVersionRecorded() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);
    var servicePath = $"{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}";

    // Get service version history
    var versionHistoryUrlPath = $"{servicePath}";
    var getResponse = await this.GetVersionHistoryResponse(versionHistoryUrlPath);

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceVersionHistory>(
      SerializerOptions);

    Assert.NotNull(body);

    // Verify service version history
    Assert.Equal(
      expected: TestRootServiceName,
      actual: body.Name);
    Assert.Equal(
      expected: TestRootServiceDisplayName,
      actual: body.DisplayName);

    Assert.NotNull(body.VersionHistory);
    var actualVersionHistory = body.VersionHistory;
    Assert.Empty(actualVersionHistory);
  }

  [Fact]
  public async Task GetServiceVersionHistory_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootOnlyConfiguration);

    // Record service version for service
    var servicePath = $"{testEnvironment}/tenants/{testTenant}/services/{TestRootServiceName}";

    var timestamp1 = DateTime.UtcNow;
    var versionToRecord1 = new ServiceVersion(
      timestamp1,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.FluxHelmRelease, TestFluxHelmReleaseVersion));
    await this.RecordServiceVersion(servicePath, versionToRecord1);

    var timestamp2 = DateTime.UtcNow;
    var version2 = "v0.0.2";
    var versionToRecord2 = new ServiceVersion(
      timestamp2,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.FluxHelmRelease, version2));
    await this.RecordServiceVersion(servicePath, versionToRecord2);

    var timestamp3 = DateTime.UtcNow;
    var version3 = "v0.0.3";
    var versionToRecord3 = new ServiceVersion(
      timestamp3,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.FluxHelmRelease, version3));
    await this.RecordServiceVersion(servicePath, versionToRecord3);

    // Get service version history
    var versionHistoryUrlPath = $"{servicePath}";
    var getResponse = await this.GetVersionHistoryResponse(versionHistoryUrlPath);

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceVersionHistory>(
      SerializerOptions);

    Assert.NotNull(body);

    // Verify service version history
    Assert.Equal(
      expected: TestRootServiceName,
      actual: body.Name);
    Assert.Equal(
      expected: TestRootServiceDisplayName,
      actual: body.DisplayName);

    Assert.NotNull(body.VersionHistory);
    var actualVersionHistory = body.VersionHistory;
    Assert.Equal(
      expected: 3,
      actual: actualVersionHistory.Count);

    Assert.NotNull(actualVersionHistory[0].Item2);
    await this.VerifyTimestampedVersionSingleType(
      timestamp1, VersionCheckType.FluxHelmRelease, TestFluxHelmReleaseVersion, actualVersionHistory[0]);

    Assert.NotNull(actualVersionHistory[1].Item2);
    await this.VerifyTimestampedVersionSingleType(
      timestamp2, VersionCheckType.FluxHelmRelease, version2, actualVersionHistory[1]);

    Assert.NotNull(actualVersionHistory[2].Item2);
    await this.VerifyTimestampedVersionSingleType(
      timestamp3, VersionCheckType.FluxHelmRelease, version3, actualVersionHistory[2]);
  }

  #endregion

  #region Version History for Services within specific Tenant

  [Fact]
  public async Task GetServicesVersionHistory_NoVersionRecorded() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootChildConfiguration);
    var envTenantPath = $"{testEnvironment}/tenants/{testTenant}";

    // Get services version history
    var versionHistoryUrlPath = $"{envTenantPath}";
    var getResponse = await this.GetVersionHistoryResponse(versionHistoryUrlPath);

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceVersionHistory[]>(
      VersionHistoryControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);

    // Verify services version history
    Assert.Empty(body);
  }

  [Fact]
  public async Task GetServicesVersionHistory_Success() {
    var (testEnvironment, testTenant) =
      await this.CreateTestConfiguration(TestRootChildConfiguration);
    var envTenantPath = $"{testEnvironment}/tenants/{testTenant}";

    // Record service version for each service
    var timestamp1 = DateTime.UtcNow;
    var rootServicePath = $"{envTenantPath}/services/{TestRootServiceName}";
    var rootServiceVersion = new ServiceVersion(
      timestamp1,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.FluxKustomization, TestFluxKustomizationVersion)
        .Add(VersionCheckType.FluxHelmRelease, TestFluxHelmReleaseVersion));

    await this.RecordServiceVersion(rootServicePath, rootServiceVersion);

    var timestamp2 = DateTime.UtcNow;
    var childServicePath = $"{envTenantPath}/services/{TestChildServiceName}";
    var childServiceVersion1 = new ServiceVersion(
      timestamp2,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.HttpResponseBody, TestHttpResponseVersion));

    await this.RecordServiceVersion(childServicePath, childServiceVersion1);

    var timestamp3 = DateTime.UtcNow;
    var httpResponseVersion2 = "v1.2.4";
    var childServiceVersion2 = new ServiceVersion(
      timestamp3,
      ImmutableDictionary<VersionCheckType, String>.Empty
        .Add(VersionCheckType.HttpResponseBody, httpResponseVersion2));

    await this.RecordServiceVersion(childServicePath, childServiceVersion2);

    // Get services version history
    var versionHistoryUrlPath = $"{envTenantPath}";
    var getResponse = await this.GetVersionHistoryResponse(versionHistoryUrlPath);

    Assert.Equal(
      expected: HttpStatusCode.OK,
      actual: getResponse.StatusCode);

    var body = await getResponse.Content.ReadFromJsonAsync<ServiceVersionHistory[]>(
      VersionHistoryControllerIntegrationTests.SerializerOptions
    );

    Assert.NotNull(body);

    // Verify services version history
    Assert.Equal(
      expected: 2,
      actual: body.Length);

    foreach (var serviceVersion in body) {
      if (serviceVersion.Name == TestRootServiceName) {

        // Verify root service's version history
        Assert.Equal(
          expected: TestRootServiceDisplayName,
          actual: serviceVersion.DisplayName);

        Assert.NotNull(serviceVersion.VersionHistory);
        var rootServiceVersionHistory = serviceVersion.VersionHistory;
        Assert.Single(rootServiceVersionHistory);

        Assert.Equal(
          expected: timestamp1.TruncateNanoseconds(),
          actual: rootServiceVersionHistory[0].Item1.TruncateNanoseconds());

        Assert.NotNull(rootServiceVersionHistory[0].Item2);
        var rootVersionHistoryInfo = rootServiceVersionHistory[0].Item2;

        Assert.Equal(
          expected: 2,
          actual: rootVersionHistoryInfo.Count);

        foreach (var serviceVersionType in rootVersionHistoryInfo) {
          if (serviceVersionType.VersionType == VersionCheckType.FluxHelmRelease) {
            Assert.Equal(
              expected: TestFluxHelmReleaseVersion,
              actual: serviceVersionType.Version);
          } else if (serviceVersionType.VersionType == VersionCheckType.FluxKustomization) {
            Assert.Equal(
              expected: TestFluxKustomizationVersion,
              actual: serviceVersionType.Version);
          }
        }
      } else if (serviceVersion.Name == TestChildServiceName) {

        // Verify child service's version history
        Assert.Equal(
          expected: TestChildServiceName,
          actual: serviceVersion.Name);
        Assert.Equal(
          expected: TestChildServiceDisplayName,
          actual: serviceVersion.DisplayName);

        Assert.NotNull(serviceVersion.VersionHistory);
        var childServiceVersionHistory = serviceVersion.VersionHistory;
        Assert.Equal(
          expected: 2,
          actual: childServiceVersionHistory.Count);

        Assert.NotNull(childServiceVersionHistory[0].Item2);
        await this.VerifyTimestampedVersionSingleType(
          timestamp2, VersionCheckType.HttpResponseBody, TestHttpResponseVersion, childServiceVersionHistory[0]);

        Assert.NotNull(childServiceVersionHistory[1].Item2);
        await this.VerifyTimestampedVersionSingleType(
          timestamp3, VersionCheckType.HttpResponseBody, httpResponseVersion2, childServiceVersionHistory[1]);
      }
    }
  }

  #endregion

  private async Task RecordServiceVersion(
    String servicePath,
    ServiceVersion versionToRecord) {

    var postResponse = await
      this.Fixture
        .CreateAdminRequest($"/api/v2/version/{servicePath}")
        .AddHeader(name: "Accept", value: "application/json")
        .And(req => {
          req.Content = JsonContent.Create(versionToRecord);
        })
        .PostAsync();

    Assert.True(
      postResponse.IsSuccessStatusCode,
      userMessage: $"Failed to record service version. Response code: {(Int32)postResponse.StatusCode}"
    );
  }

  private async Task<HttpResponseMessage> GetVersionHistoryResponse(
    String versionHistoryUrlPath) {
    var getResponse = await
      this.Fixture.Server
        .CreateRequest($"/api/v2/version-history/{versionHistoryUrlPath}")
        .AddHeader(name: "Accept", value: "application/json")
        .GetAsync();

    return getResponse;
  }

  private async Task VerifyTimestampedVersionSingleType(
    DateTime expectedTimestamp,
    VersionCheckType versionType,
    String versionValue,
    ValueTuple<DateTime, IImmutableList<ServiceVersionTypeInfo>> listOfVersions) {

    Assert.Equal(
      expected: expectedTimestamp.TruncateNanoseconds(),
      actual: listOfVersions.Item1.TruncateNanoseconds());
    Assert.Single(listOfVersions.Item2);
    Assert.Equal(
      expected: versionType,
      actual: listOfVersions.Item2[0].VersionType);
    Assert.Equal(
      expected: versionValue,
      actual: listOfVersions.Item2[0].Version);
  }
}
