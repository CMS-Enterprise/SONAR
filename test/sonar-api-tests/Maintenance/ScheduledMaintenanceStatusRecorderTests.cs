using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Cms.BatCave.Sonar.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests.Maintenance;

public class ScheduledMaintenanceStatusRecorderTests : MaintenanceStatusTestsBase {

  public ScheduledMaintenanceStatusRecorderTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper outputHelper)
    : base(fixture, outputHelper) { }

  [Fact]
  public async Task RecordAsync_NoScheduledMaintenanceRecords_PrometheusIsNotCalled() {
    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(10), ct);

      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledTenantMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(10), ct);

      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(10), ct);
    });

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
  public async Task RecordAsync_InactiveScheduledMaintenance_PrometheusIsNotCalled() {
    await this.AddMaintenanceAsync(ScheduledEnvironmentMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(1),
      scheduleTimeZone: "UTC",
      durationMinutes: 120,
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    this.MockPrometheusService.Verify(
      expression: p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task RecordAsync_ActiveScheduledEnvironmentMaintenance_ExpectedServicesPassedToPrometheus() {
    await this.AddMaintenanceAsync(ScheduledEnvironmentMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-1),
      scheduleTimeZone: "UTC",
      durationMinutes: 120,
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    List<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) =>
        serviceMaintenancesToRecord.AddRange(serviceMaintenances));

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-2",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1-child",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1-child",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ActiveScheduledTenantMaintenance_ExpectedServicesPassedToPrometheus() {
    await this.AddMaintenanceAsync(ScheduledTenantMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-1),
      scheduleTimeZone: "UTC",
      durationMinutes: 120,
      tenantId: this.TestServiceHierarchyIds["environment-1/tenant-2"]));

    List<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) =>
        serviceMaintenancesToRecord.AddRange(serviceMaintenances));

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledTenantMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1",
        MaintenanceScope = "tenant",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "tenant",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1-child",
        MaintenanceScope = "tenant",
        MaintenanceType = "scheduled"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ActiveScheduledServiceMaintenance_ExpectedServicesPassedToPrometheus() {
    await this.AddMaintenanceAsync(ScheduledServiceMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-1),
      scheduleTimeZone: "UTC",
      durationMinutes: 120,
      serviceId: this.TestServiceHierarchyIds["environment-2/tenant-1/service-2"]));

    List<ServiceMaintenance> serviceMaintenancesToRecord = new();

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .Callback((IImmutableList<ServiceMaintenance> serviceMaintenances, CancellationToken _) =>
        serviceMaintenancesToRecord.AddRange(serviceMaintenances));

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-2",
        TenantName = "tenant-1",
        ServiceName = "service-2",
        MaintenanceScope = "service",
        MaintenanceType = "scheduled"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ServicesInMultipleMaintenanceScopes_AllScopesRecorded() {
    await this.AddMaintenanceAsync(ScheduledEnvironmentMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-5),
      scheduleTimeZone: "UTC",
      durationMinutes: 10 * 60,
      environmentId: this.TestServiceHierarchyIds["environment-1"]));

    await this.AddMaintenanceAsync(ScheduledTenantMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-3),
      scheduleTimeZone: "UTC",
      durationMinutes: 6 * 60,
      tenantId: this.TestServiceHierarchyIds["environment-1/tenant-2"]));

    await this.AddMaintenanceAsync(ScheduledServiceMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-1),
      scheduleTimeZone: "UTC",
      durationMinutes: 2 * 60,
      serviceId: this.TestServiceHierarchyIds["environment-1/tenant-2/service-2"]));

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

    var environmentRecorderTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledEnvironmentMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var tenantRecorderTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledTenantMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    var serviceRecorderTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledServiceMaintenance>>()
        .RecordAsync(DateTime.UtcNow, TimeSpan.FromSeconds(5), ct);
    });

    await Task.WhenAll(environmentRecorderTask, tenantRecorderTask, serviceRecorderTask);

    var expectedServiceMaintenances = new List<Object> {
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-2",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1",
        MaintenanceScope = "tenant",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "tenant",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-2",
        MaintenanceScope = "service",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-1",
        ServiceName = "service-1-child",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1-child",
        MaintenanceScope = "environment",
        MaintenanceType = "scheduled"
      },
      new {
        EnvironmentName = "environment-1",
        TenantName = "tenant-2",
        ServiceName = "service-1-child",
        MaintenanceScope = "tenant",
        MaintenanceType = "scheduled"
      }
    };

    Assert.Equal(expected: expectedServiceMaintenances.Count, actual: serviceMaintenancesToRecord.Count);
    Assert.Equivalent(expected: expectedServiceMaintenances, actual: serviceMaintenancesToRecord, strict: false);
  }

  [Fact]
  public async Task RecordAsync_ExceptionIsThrownWhileRecording_MaintenanceRecordsAreUnlockedAndExceptionIsReThrown() {
    await this.AddMaintenanceAsync(ScheduledServiceMaintenance.New(
      scheduleExpression: GetDailyScheduleExpressionUtc(-1),
      scheduleTimeZone: "UTC",
      durationMinutes: 120,
      serviceId: this.TestServiceHierarchyIds["environment-2/tenant-2/service-2"]));

    var lastRecorded = DateTime.UtcNow;
    var expectedError = new Exception($"The test exception happened at approximately {lastRecorded} UTC");

    this.MockPrometheusService
      .Setup(p => p.WriteServiceMaintenanceStatusAsync(
        It.IsAny<IImmutableList<ServiceMaintenance>>(),
        It.IsAny<CancellationToken>()))
      .ThrowsAsync(expectedError);

    var recordAsyncTask = this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      await serviceProvider.GetRequiredService<ScheduledMaintenanceStatusRecorder<ScheduledServiceMaintenance>>()
        .RecordAsync(lastRecorded, TimeSpan.FromSeconds(5), ct);
    });

    var actualError = await Assert.ThrowsAsync<Exception>(() => recordAsyncTask);

    Assert.Equivalent(expectedError, actualError);

    await this.Fixture.WithDependenciesAsync(async (serviceProvider, ct) => {
      var maintenance = await serviceProvider.GetRequiredService<MaintenanceDataHelper<ScheduledServiceMaintenance>>()
        .SingleOrDefaultByAssocEntityIdAsync(this.TestServiceHierarchyIds["environment-2/tenant-2/service-2"], ct);

      Assert.False(maintenance!.IsRecording);
      Assert.Equal(lastRecorded, (DateTime)maintenance.LastRecorded!, TimeSpan.FromMicroseconds(1));
    });
  }

  /// <summary>
  /// Returns a cron expression that occurs daily at the current hour, or at the specified offset
  /// from the current hour (in UTC).
  /// </summary>
  private static String GetDailyScheduleExpressionUtc(Int32 hourOffset = 0) {
    var hour = (DateTime.UtcNow.Hour + (hourOffset % 24) + 24) % 24;
    return $"0 {hour} * * *";
  }

}
