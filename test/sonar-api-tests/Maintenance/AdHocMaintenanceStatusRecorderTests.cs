using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests.Maintenance;

public class AdHocMaintenanceStatusRecorderTests : MaintenanceStatusTestsBase {

  public AdHocMaintenanceStatusRecorderTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper output)
    : base(fixture, output) { }

  [Fact]
  public async Task RecordAsync_NoActiveAdHocMaintenances_PrometheusIsNotCalled() {
    // Arrange
    // By default in this test class, there are no active maintenance records in the DB, so there's nothing special to
    // arrange. There is already a service hierarchy set up that all tests in this class have access to (see the base
    // class) - we'll assert these exist as a way of showing that prometheus was uncalled due to lack of maintenance
    // records, not due to lack of services.

    // Act
    // Call ALL of the ad-hoc maintenance status recorders with the expectation that they'll all do a no-op.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);

      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocTenantMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);

      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    // Assert
    // Assert that the prometheus service was never called because there was no active maintenance status to record.
    // Also assert that services do actually exist to record status for had there been any active maintenance.
    this.MockPrometheusService.Verify(
      expression: p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()),
      Times.Never);

    this.Fixture.WithDependencies(serviceProvider => {
      var environments = serviceProvider.GetRequiredService<DbSet<Environment>>();
      var tenants = serviceProvider.GetRequiredService<DbSet<Tenant>>();
      var services = serviceProvider.GetRequiredService<DbSet<Service>>();

      Assert.NotEmpty(environments.ToList());
      Assert.NotEmpty(tenants.ToList());
      Assert.NotEmpty(services.ToList());
    });
  }

  [Fact]
  public async Task RecordAsync_ActiveAdHocEnvironmentMaintenance_ExpectedServicesPassedToPrometheus() {
    // Arrange
    // Add an ad-hoc environment maintenance record to the DB, and set up the prometheus service mock to capture the
    // service maintenances passed to it to be recorded.
    await this.AddMaintenanceAsync(AdHocEnvironmentMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    List<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) =>
        serviceMaintenancesToRecord.AddRange(serviceMaintenances));

    // Act
    // Call the ad-hoc environment maintenance status recorder with the expectation that it will pick up the
    // maintenance record set up above and pass the appropriate services to prometheus to record status for.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    // Assert
    // Assert that exactly the set of services we expect are got to prometheus for recording.
    // Using Assert.Equivalent here which allows objects of different types to be compared by public fields
    // and properties that have the same name. Using the non-strict variant of Assert.Equivalent to allow the
    // actuals to have more fields than the expecteds, as long as the expecteds match. It's a pretty neat and
    // useful method for comparing collections.
    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1",
        MaintenanceScope = "environment",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-2",
        MaintenanceScope = "environment",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1",
        MaintenanceScope = "environment",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "environment",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1-child",
        MaintenanceScope = "environment",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1-child",
        MaintenanceScope = "environment",
        MaintenanceType = "adhoc"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ActiveAdHocTenantMaintenance_ExpectedServicesPassedToPrometheus() {
    // Arrange
    // Add an ad-hoc tenant maintenance record to the DB, and set up the prometheus service mock to capture the
    // service maintenances passed to it to be recorded.
    await this.AddMaintenanceAsync(AdHocTenantMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      tenantId: this.TestServiceHierarchyIds["environment-2/tenant-2"]));

    List<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) =>
        serviceMaintenancesToRecord.AddRange(serviceMaintenances));

    // Act
    // Call the ad-hoc tenant maintenance status recorder with the expectation that it will pick up the
    // maintenance record set up above and pass the appropriate services to prometheus to record status for.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocTenantMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    // Assert
    // Assert that exactly the set of services we expect got passed to prometheus for recording.
    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-2",
        TenantName = "tenant-2",
        ServiceName = "service-1",
        MaintenanceScope = "tenant",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-2",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "tenant",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-2",
        TenantName = "tenant-2",
        ServiceName = "service-1-child",
        MaintenanceScope = "tenant",
        MaintenanceType = "adhoc"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ActiveAdHocServiceMaintenance_ExpectedServicesPassedToPrometheus() {
    // Arrange
    // Add an ad-hoc service maintenance record to the DB, and set up the prometheus service mock to capture the
    // service maintenances passed to it to be recorded.
    await this.AddMaintenanceAsync(AdHocServiceMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      serviceId: this.TestServiceHierarchyIds["environment-2/tenant-1/service-2"]));

    List<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) =>
        serviceMaintenancesToRecord.AddRange(serviceMaintenances));

    // Act
    // Call the ad-hoc service maintenance status recorder with the expectation that it will pick up the
    // maintenance record set up above and pass the appropriate services to prometheus to record status for.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    // Assert
    // Assert that exactly the set of services we expect got passed to prometheus for recording.
    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-2",
        TenantName = "tenant-1",
        ServiceName = "service-2",
        MaintenanceScope = "service",
        MaintenanceType = "adhoc"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ServiceInTwoMaintenanceScopes_BothScopesRecorded() {
    // Arrange
    // Add an ad-hoc tenant maintenance record and an ad-hoc service maintenance record to the DB where an individual
    // service is covered by both maintenances. Also set up the prometheus service mock to capture the
    // service maintenances passed to it to be recorded.
    await this.AddMaintenanceAsync(AdHocTenantMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      tenantId: this.TestServiceHierarchyIds["environment-1/tenant-1"]));

    await this.AddMaintenanceAsync(AdHocServiceMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      serviceId: this.TestServiceHierarchyIds["environment-1/tenant-1/service-1"]));

    // Use a concurrent collection to capture the arguments passed to prometheus service because it will be called
    // by two concurrent threads.
    ConcurrentBag<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) => {
        foreach (var serviceMaintenance in serviceMaintenances) {
          serviceMaintenancesToRecord.Add(serviceMaintenance);
        }
      });

    // Act
    // Invoke both tenant and service maintenance status recorders concurrently with the expectation that they'll
    // pick up the maintenance records set up above and pass the appropriate services to prometheus to record status.
    var tenantRecorderTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocTenantMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var serviceRecorderTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    await Task.WhenAll(tenantRecorderTask, serviceRecorderTask);

    // Assert
    // Assert that exactly the set of services we expect got passed to prometheus for recording.
    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1",
        MaintenanceScope = "tenant",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-2",
        MaintenanceScope = "tenant",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1",
        MaintenanceScope = "service",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1-child",
        MaintenanceScope = "tenant",
        MaintenanceType = "adhoc"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1-child",
        MaintenanceScope = "service",
        MaintenanceType = "adhoc"
      },
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ConcurrentExecutionsForSameMaintenanceScope_OnlyOneRecords() {
    // Arrange
    // Add an ad-hoc service maintenance record to the DB.
    await this.AddMaintenanceAsync(AdHocServiceMaintenance.New(
      appliedByUserId: this.TestUserIds["user-2"],
      startTime: DateTime.UtcNow,
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      serviceId: this.TestServiceHierarchyIds["environment-1/tenant-1/service-1"]));

    // Act
    // Invoke two ad-hoc service maintenance status recorders concurrently (as if by two separate sonar-api pods).
    var recorderTask1 = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var recorderTask2 = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    await Task.WhenAll(recorderTask1, recorderTask2);

    // Assert
    // Assert that only one of the recorders actually called the prometheus service to record the maintenance status
    // and the other one did not (only one of them should be able to lock the maintenance record).
    this.MockPrometheusService.Verify(
      expression: p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task RecordAsync_RecordingLockTest() {
    // This test makes a best-effort attempt at validating the locking behavior of the maintenance status recorder by
    // trying to force some synchronization, but it's still pretty timing-dependent, which can be fragile. If it
    // turns out to be flaky when run in the CI pipeline, it can be skipped.

    // Insert a maintenance record that will get locked by the recorder.
    await this.AddMaintenanceAsync(AdHocEnvironmentMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)),
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    // Set up the prometheus service to delay a bit so we have a chance to check the recording lock mid-operation,
    // using a cancellation token for synchronization. When the mock is called, it will return a task that delays for 1
    // second. At the same time, the mock will fire the callback that cancels the gate token, at which point we'll know
    // recorder is in the middle of its operation (waiting on prometheus) and we can check the lock during the delay.
    using var gate = new CancellationTokenSource();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Returns(Task.Delay(1000))
      .Callback(gate.Cancel);

    // Before we start the RecordAsync task, assert a pre-condition that the maintenance record hasn't been locked yet.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var maintenance = await serviceProvider.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>()
        .SingleAsync(ct);
      Assert.False(maintenance.IsRecording); // Not yet recording
      Assert.Null(maintenance.LastRecorded); // Last recorded time has not been set yet
    });

    // Start the recording task, but don't await it on this thread. Instead, let this thread wait on the gate token
    // cancellation that happens when the mock is called.
    var expectedLastRecorded = DateTime.UtcNow;
    var recordingTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocEnvironmentMaintenance>>()
        .RecordAsync(expectedLastRecorded, TimeSpan.FromSeconds(10), ct);
    });

    try {
      await Task.Delay(2000, gate.Token);
      Assert.Fail("The gate token was not cancelled as expected.");
    } catch (TaskCanceledException) {
      // Synchronization gate, the cancellation exception is expected.
    }

    // At this point, we know the mock has been called, so we are in the middle of recording, and we can assert that
    // the maintenance record has now been locked.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var maintenance = await serviceProvider.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>()
        .SingleAsync(ct);
      // Assert that we are now recording, and that the last recorded time has been set to our timestamp.
      // Postgres only stores timestamps with microsecond accuracy, so we allow some tiny slop in time time comparison.
      Assert.True(maintenance.IsRecording);
      Assert.Equal(expectedLastRecorded, (DateTime)maintenance.LastRecorded!, TimeSpan.FromMicroseconds(1));
    });

    // Now await the remainder of the mock delay on this thread.
    await recordingTask;

    // And at this point, recording will have completed, and we can assert the recorder has released the lock.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var maintenance = await serviceProvider.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>()
        .SingleAsync(ct);
      // Assert that we are no longer recording, and that the last recorded time still reflects our timestamp.
      Assert.False(maintenance.IsRecording);
      Assert.Equal(expectedLastRecorded, (DateTime)maintenance.LastRecorded!, TimeSpan.FromMicroseconds(1));
    });
  }

  [Fact]
  public async Task RecordAsync_AdHocMaintenancePeriodHasEnded_DatabaseRecordIsDeleted() {
    // Arrange
    // Add an ad-hoc environment maintenance record to the DB who's end time has passed.
    await this.AddMaintenanceAsync(AdHocEnvironmentMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)),
      endTime: DateTime.UtcNow,
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    // Act
    // Call the ad-hoc environment maintenance status recorder with the expectation that it will pick up the
    // maintenance record set up above and pass the appropriate services to prometheus to record status for.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(10), ct);
    });

    // Assert
    // Assert that we did indeed call prometheus (the maintenance record existed when we ran the recorder), and then
    // after we were done recording the maintenance record was deleted because its end time was past.
    this.MockPrometheusService.Verify(
      expression: p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()),
      Times.Once);

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var maintenance = await serviceProvider.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>()
        .SingleOrDefaultAsync(ct);
      Assert.Null(maintenance);
    });
  }

  [Fact]
  public async Task RecordAsync_AdHocMaintenancePeriodHasNotEnded_DatabaseRecordIsNotDeleted() {
    // Arrange
    // Add an ad-hoc environment maintenance record to the DB who's end time has not passed.
    await this.AddMaintenanceAsync(AdHocEnvironmentMaintenance.New(
      appliedByUserId: this.TestUserIds["user-1"],
      startTime: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)),
      endTime: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    // Act
    // Call the ad-hoc environment maintenance status recorder with the expectation that it will pick up the
    // maintenance record set up above and pass the appropriate services to prometheus to record status for.
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<AdHocMaintenanceStatusRecorder<AdHocEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(10), ct);
    });

    // Assert
    // Assert that we did indeed call prometheus (the maintenance record existed when we ran the recorder), and then
    // after we were done recording the maintenance record was still present because its end time has not passed.
    this.MockPrometheusService.Verify(
      expression: p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()),
      Times.Once);

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var maintenance = await serviceProvider.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>()
        .SingleOrDefaultAsync(ct);
      Assert.NotNull(maintenance);
    });
  }
}
