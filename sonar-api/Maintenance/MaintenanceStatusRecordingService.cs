using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Maintenance;

public class MaintenanceStatusRecordingService : BackgroundService {
  private readonly ILogger<MaintenanceStatusRecordingService> _logger;
  private readonly IServiceProvider _serviceProvider;
  private readonly Type[] _recorderTypes;

  public MaintenanceStatusRecordingService(
    ILogger<MaintenanceStatusRecordingService> logger,
    IServiceProvider serviceProvider) {

    this._logger = logger;
    this._serviceProvider = serviceProvider;
    this._recorderTypes = GetMaintenanceStatusRecorderTypes();
  }

  // TODO make recording interval properly externally configurable and ditch the prop
  protected virtual TimeSpan RecordingInterval { get; } = TimeSpan.FromSeconds(10);

  /// <summary>
  /// Starts the background thread that periodically records maintenance status information to Prometheus
  /// for all active maintenances of types that have a recorder registered in the DI container.
  /// </summary>
  /// <param name="ct"></param>
  protected override async Task ExecuteAsync(CancellationToken ct) {
    this._logger.LogInformation("Starting maintenance status recording service.");

    do {
      this._logger.LogDebug("Executing maintenance status recorders.");
      await this.ExecuteRecordersAsync(ct);
      await Task.Delay(this.RecordingInterval, ct);
    } while (!ct.IsCancellationRequested);
  }

  protected virtual Task ExecuteRecordersAsync(CancellationToken ct) =>
    Task.WhenAll(this._recorderTypes.Select(t => this.ExecuteRecorderAsync(t, ct)));

  /// <summary>
  /// Executes the maintenance status recorder of the given type if it is registered in the DI container.
  /// The recorder is executed with its own service provider scope so that multiple recorders can be run
  /// in parallel.
  /// </summary>
  private async Task ExecuteRecorderAsync(Type recorderType, CancellationToken ct) {
    try {
      using var scope = this._serviceProvider.CreateScope();
      if (scope.ServiceProvider.GetService(recorderType) is IMaintenanceStatusRecorder recorder) {
        await recorder.RecordAsync(DateTime.UtcNow, this.RecordingInterval, ct);
      }
    } catch (Exception e) {
      this._logger.LogError(
        exception: e,
        message: "Unexpected error while executing {recorderType}.",
        recorderType.Name);
    }
  }

  /// <summary>
  /// Helper method to generate a list of the known maintenance recorder types in the assembly.
  /// </summary>
  /// <returns></returns>
  private static Type[] GetMaintenanceStatusRecorderTypes() {
    return typeof(MaintenanceStatusRecordingService).Assembly.GetTypes()
      .Where(t => t.IsClass && t.IsSubclassOf(typeof(ScheduledMaintenance)) && !t.IsAbstract)
      .Select(t => typeof(ScheduledMaintenanceStatusRecorder<>).MakeGenericType(t))
      .ToArray()
      .Union(typeof(MaintenanceStatusRecordingService).Assembly.GetTypes()
        .Where(t => t.IsClass && t.IsSubclassOf(typeof(AdHocMaintenance)) && !t.IsAbstract)
        .Select(t => typeof(AdHocMaintenanceStatusRecorder<>).MakeGenericType(t))
        .ToArray())
      .ToArray();
  }
}

/// <summary>
/// Extension methods for registering maintenance status recording services in the DI container.
/// Maintenance status recording for any maintenance type and scope can be disabled by simply not registering
/// it in the container.
/// </summary>
public static class MaintenanceStatusRecordingServicesExtensions {
  public static IServiceCollection AddMaintenanceStatusRecordingService(this IServiceCollection services) {
    return services
      .AddScoped<ScheduledMaintenanceStatusRecorder<ScheduledEnvironmentMaintenance>>()
      .AddScoped<ScheduledMaintenanceStatusRecorder<ScheduledTenantMaintenance>>()
      .AddScoped<ScheduledMaintenanceStatusRecorder<ScheduledServiceMaintenance>>()
      .AddScoped<AdHocMaintenanceStatusRecorder<AdHocEnvironmentMaintenance>>()
      .AddScoped<AdHocMaintenanceStatusRecorder<AdHocTenantMaintenance>>()
      .AddScoped<AdHocMaintenanceStatusRecorder<AdHocServiceMaintenance>>()
      .AddHostedService<MaintenanceStatusRecordingService>();
  }
}
