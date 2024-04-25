using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Maintenance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests.Maintenance;

public class MaintenanceStatusRecordingServiceTests : MaintenanceStatusTestsBase {

  // A test delay for the GitLab CI/CD pipeline runners, where parallel tests seem to run more slowly
  // and sometimes background threads don't gets as much CPU share as on a local developer machine.
  private readonly TimeSpan _testDelay = TimeSpan.FromSeconds(5);

  public MaintenanceStatusRecordingServiceTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper output)
    : base(fixture, output) { }

  [Fact]
  public async Task ExecuteAsync_ExpectedStatusesRecorded() {
    await Task.WhenAll(
      this.AddMaintenanceAsync(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 0 * * *",
          scheduleTimeZone: "UTC",
          durationMinutes: 24 * 60,
          environmentId: this.TestServiceHierarchyIds["environment-1"])),
      this.AddMaintenanceAsync(
        ScheduledTenantMaintenance.New(
          scheduleExpression: "0 0 * * *",
          scheduleTimeZone: "UTC",
          durationMinutes: 24 * 60,
          tenantId: this.TestServiceHierarchyIds["environment-2/tenant-1"])),
      this.AddMaintenanceAsync(
        ScheduledServiceMaintenance.New(
          scheduleExpression: "0 0 * * *",
          scheduleTimeZone: "UTC",
          durationMinutes: 24 * 60,
          serviceId: this.TestServiceHierarchyIds["environment-1/tenant-2/service-1"])),
      this.AddMaintenanceAsync(
        AdHocEnvironmentMaintenance.New(
          appliedByUserId: this.TestUserIds["user-1"],
          startTime: DateTime.UtcNow,
          endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
          environmentId: this.TestServiceHierarchyIds["environment-2"])),
      this.AddMaintenanceAsync(
        AdHocTenantMaintenance.New(
          appliedByUserId: this.TestUserIds["user-2"],
          startTime: DateTime.UtcNow,
          endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
          tenantId: this.TestServiceHierarchyIds["environment-1/tenant-2"])),
      this.AddMaintenanceAsync(
        AdHocServiceMaintenance.New(
          appliedByUserId: this.TestUserIds["user-1"],
          startTime: DateTime.UtcNow,
          endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
          serviceId: this.TestServiceHierarchyIds["environment-2/tenant-1/service-2"]))
    );

    var serviceMaintenanceBatchesRecorded = new ConcurrentBag<IImmutableList<ServiceMaintenance>>();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> records, CancellationToken ct) =>
        serviceMaintenanceBatchesRecorded.Add(records));

    var cts = new CancellationTokenSource(this._testDelay);

    try {
      var pod1Task = this.Fixture.WithDependenciesAsync(action: (serviceProvider, _) =>
        new MaintenanceStatusRecordingService0(
            serviceProvider.GetRequiredService<ILogger<MaintenanceStatusRecordingService>>(),
            serviceProvider)
          .ExecuteDirectlyAsync(cts.Token));

      var pod2Task = this.Fixture.WithDependenciesAsync(action: (serviceProvider, _) =>
        new MaintenanceStatusRecordingService0(
            serviceProvider.GetRequiredService<ILogger<MaintenanceStatusRecordingService>>(),
            serviceProvider)
          .ExecuteDirectlyAsync(cts.Token));

      await Task.WhenAll(pod1Task, pod2Task);
    } catch (OperationCanceledException) {
      // expected
    }

    // Two recording processes were run concurrently in the same recording period, but only one of them should have
    // handled recording for any of the given maintenance configurations. Ensure this is true by asserting the total
    // number of batches was 6 (1 for each maintenance configuration inserted above), and that the total number of
    // statuses across all batches was 21 (6 for ad-hoc env, 6 for scheduled env, 3 for ad-hoc tenant, 3 for scheduled
    // tenant, 1 for ad-hoc service, 2 for scheduled service).
    Assert.Equal(expected: 6, serviceMaintenanceBatchesRecorded.Count);
    Assert.Equal(expected: 21, serviceMaintenanceBatchesRecorded.SelectMany(s => s).Count());
  }

  /// <summary>
  /// Test-only derived version of MaintenanceStatusRecordingService.
  /// </summary>
  private class MaintenanceStatusRecordingService0 : MaintenanceStatusRecordingService {
    private Int32 _iterations = 1;
    private readonly ILogger<MaintenanceStatusRecordingService> _logger;

    public MaintenanceStatusRecordingService0(
      ILogger<MaintenanceStatusRecordingService> logger,
      IServiceProvider serviceProvider)
      : base(logger, serviceProvider) {
      this._logger = logger;
    }

    public Task ExecuteDirectlyAsync(CancellationToken ct) => this.ExecuteAsync(ct);

    protected override TimeSpan RecordingInterval { get; } = TimeSpan.FromSeconds(1);

    // The intent of this test-only version of the service is to run multiple instances concurrently to simulate
    // multiple sonar-api pods, but only have them each run one "working" iteration around the recording loop and
    // have any others that are able to occur in the test time frame be no-ops in order to invoke a predictable number
    // of actual recorder tasks.
    protected override Task ExecuteRecordersAsync(CancellationToken ct) {
      this._logger.LogInformation($"Iteration {this._iterations}");
      return this._iterations-- > 0 ? base.ExecuteRecordersAsync(ct) : Task.CompletedTask;
    }

  }

}
