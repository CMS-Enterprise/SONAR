using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Helpers.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
using Environment = Cms.BatCave.Sonar.Data.Environment;

namespace Cms.BatCave.Sonar.Tests.Helpers.Maintenance;

public class MaintenanceDataHelpersTests : ApiControllerTestsBase {

  private readonly ITestOutputHelper _output;

  public MaintenanceDataHelpersTests(
    ApiIntegrationTestFixture fixture,
    ITestOutputHelper output)
    : base(fixture, output, resetDatabase: true) {
    this._output = output;
  }

  [Fact]
  public async Task LiveTest() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {

      var environmentsTable = services.GetRequiredService<DbSet<Environment>>();
      var usersTable = services.GetRequiredService<DbSet<User>>();
      var scheduledEnvironmentMaintenancesTable = services.GetRequiredService<DbSet<ScheduledEnvironmentMaintenance>>();
      var adHocEnvironmentMaintenancesTable = services.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>();
      var dataContext = services.GetRequiredService<DataContext>();

      var environment = environmentsTable.Add(
        Environment.New(name: "test", isNonProduction: true));

      var user = usersTable.Add(
        User.New(email: "user@host", fullName: "test"));

      scheduledEnvironmentMaintenancesTable.Add(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 0 ? * SUN,SAT",
          scheduleTimeZone: "US/Eastern",
          durationMinutes: 1440,
          environment.Entity.Id));

      scheduledEnvironmentMaintenancesTable.Add(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 12 ? * MON-FRI",
          scheduleTimeZone: "US/Eastern",
          durationMinutes: 60,
          environment.Entity.Id));

      adHocEnvironmentMaintenancesTable.Add(
        AdHocEnvironmentMaintenance.New(
          appliedByUserId: user.Entity.Id,
          startTime: DateTime.UtcNow,
          endTime: DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
          environmentId: environment.Entity.Id));

      await dataContext.SaveChangesAsync(cancellationToken);

      var scheduledEnvironmentMaintenanceDataHelper =
        services.GetRequiredService<MaintenanceDataHelper<ScheduledEnvironmentMaintenance>>();

      var scheduledMaintenances = await scheduledEnvironmentMaintenanceDataHelper.FindAllByAssocEntityIdAsync(
        environment.Entity.Id,
        cancellationToken);

      foreach (var maintenance in scheduledMaintenances) {
        this._output.WriteLine(
          $"EnvironmentId='{maintenance.EnvironmentId}' " +
          $"ScheduleExpression='{maintenance.ScheduleExpression}' " +
          $"ScheduleTimeZone='{maintenance.ScheduleTimeZone}' " +
          $"DurationMinutes={maintenance.DurationMinutes}");
      }

      var adHocEnvironmentMaintenanceDataHelper =
        services.GetRequiredService<MaintenanceDataHelper<AdHocEnvironmentMaintenance>>();

      var adHocMaintenance = (await adHocEnvironmentMaintenanceDataHelper.SingleOrDefaultByAssocEntityIdAsync(
        environment.Entity.Id,
        cancellationToken))!;

      this._output.WriteLine(
        $"EnvironmentId='{adHocMaintenance.EnvironmentId}' " +
        $"AppliedByUserId='{adHocMaintenance.AppliedByUserId}', " +
        $"StartTime={adHocMaintenance.StartTime} " +
        $"EndTime={adHocMaintenance.EndTime}");

    });
  }

  [Fact]
  public async Task ConstraintViolationTest() {
    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var environmentsTable = services.GetRequiredService<DbSet<Environment>>();
      var usersTable = services.GetRequiredService<DbSet<User>>();
      var dataContext = services.GetRequiredService<DataContext>();

      environmentsTable.Add(Environment.New(name: "test", isNonProduction: true));
      usersTable.Add(User.New(email: "user1@host", fullName: "test user1"));
      usersTable.Add(User.New(email: "user2@host", fullName: "test user2"));

      await dataContext.SaveChangesAsync(cancellationToken);
    });

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var environmentsTable = services.GetRequiredService<DbSet<Environment>>();
      var usersTable = services.GetRequiredService<DbSet<User>>();
      var adHocEnvironmentMaintenancesTable = services.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>();
      var dataContext = services.GetRequiredService<DataContext>();

      var environment = environmentsTable.Single(e => e.Name == "test");
      var user1 = usersTable.Single(u => u.Email == "user1@host");

      adHocEnvironmentMaintenancesTable.Add(
        AdHocEnvironmentMaintenance.New(
          appliedByUserId: user1.Id,
          startTime: DateTime.UtcNow,
          endTime: DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
          environmentId: environment.Id));

      await dataContext.SaveChangesAsync(cancellationToken);
    });

    await this.Fixture.WithDependenciesAsync(async (services, cancellationToken) => {
      var environmentsTable = services.GetRequiredService<DbSet<Environment>>();
      var usersTable = services.GetRequiredService<DbSet<User>>();
      var adHocEnvironmentMaintenancesTable = services.GetRequiredService<DbSet<AdHocEnvironmentMaintenance>>();
      var dataContext = services.GetRequiredService<DataContext>();

      var environment = environmentsTable.Single(e => e.Name == "test");
      var user2 = usersTable.Single(u => u.Email == "user2@host");

      adHocEnvironmentMaintenancesTable.Add(
        AdHocEnvironmentMaintenance.New(
          appliedByUserId: user2.Id,
          startTime: DateTime.UtcNow,
          endTime: DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
          environmentId: environment.Id));

      try {
        await dataContext.SaveChangesAsync(cancellationToken);
      } catch (DbUpdateException e) when (e.InnerException is PostgresException {
        SqlState: "23505",
        ConstraintName: "ix_ad_hoc_maintenance_environment_id"
      }) {
        this._output.WriteLine("Cannot have more than one ad-hoc maintenance record for a single environment.");
      }

      var maintenanceDataHelper = services.GetRequiredService<MaintenanceDataHelper<AdHocEnvironmentMaintenance>>();

      var adHocMaintenance = (await maintenanceDataHelper.SingleOrDefaultByAssocEntityIdAsync(
        environment.Id,
        cancellationToken))!;

      this._output.WriteLine(
        $"EnvironmentId='{adHocMaintenance.EnvironmentId}' " +
        $"AppliedByUserId='{adHocMaintenance.AppliedByUserId}', " +
        $"StartTime={adHocMaintenance.StartTime} " +
        $"EndTime={adHocMaintenance.EndTime}");
    });
  }

  [Fact]
  public async Task FindAllByAssocEntityIdsAsyncTest() {
    await this.Fixture.WithDependenciesAsync(async (services, ct) => {

      var environments = services.GetRequiredService<DbSet<Environment>>();
      var scheduledEnvironmentMaintenances = services.GetRequiredService<DbSet<ScheduledEnvironmentMaintenance>>();
      var dataContext = services.GetRequiredService<DataContext>();

      // Create three environments
      var environment1 = environments.Add(Environment.New(name: "test1", isNonProduction: true)).Entity;
      var environment2 = environments.Add(Environment.New(name: "test2", isNonProduction: true)).Entity;
      var environment3 = environments.Add(Environment.New(name: "test3", isNonProduction: true)).Entity;

      // Create scheduled maintenance config for each environment
      scheduledEnvironmentMaintenances.Add(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 0 ? * SUN,SAT",
          scheduleTimeZone: "US/Eastern",
          durationMinutes: 1440,
          environmentId: environment1.Id));

      scheduledEnvironmentMaintenances.Add(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 0 ? * SUN,SAT",
          scheduleTimeZone: "US/Eastern",
          durationMinutes: 1440,
          environmentId: environment1.Id));

      scheduledEnvironmentMaintenances.Add(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 0 ? * SUN,SAT",
          scheduleTimeZone: "US/Eastern",
          durationMinutes: 1440,
          environmentId: environment2.Id));

      scheduledEnvironmentMaintenances.Add(
        ScheduledEnvironmentMaintenance.New(
          scheduleExpression: "0 0 ? * SUN,SAT",
          scheduleTimeZone: "US/Eastern",
          durationMinutes: 1440,
          environmentId: environment3.Id));

      await dataContext.SaveChangesAsync(ct);

      // Query for the scheduled maintenances matching a subset of the environment ids
      var environmentIds = new[] { environment1.Id, environment2.Id };

      var scheduledMaintenances =
        await services.GetRequiredService<MaintenanceDataHelper<ScheduledEnvironmentMaintenance>>()
          .FindAllByAssocEntityIdsAsync(environmentIds, ct);

      // Assert that we only got back maintenances matching the environments we queried for
      Assert.Equal(expected: 3, scheduledMaintenances.Count);
      foreach (var scheduledMaintenance in scheduledMaintenances) {
        Assert.Contains(scheduledMaintenance.EnvironmentId, environmentIds);
      }
    });
  }

}
