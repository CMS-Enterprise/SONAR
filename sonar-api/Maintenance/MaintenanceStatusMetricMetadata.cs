using System;
using Prometheus;

namespace Cms.BatCave.Sonar.Maintenance;

public static class MaintenanceStatusMetricMetadata {
  public const String MetricFamilyName = "sonar_service_maintenance_status";
  public const String EnvironmentLabel = "environment";
  public const String TenantLabel = "tenant";
  public const String ServiceLabel = "service";
  public const String MaintenanceScopeLabel = "maintenance_scope";
  public const String MaintenanceTypeLabel = "maintenance_type";

  public static readonly MetricMetadata MetricMetadata = new() {
    Help = "Whether a given service is in maintenance",
    Type = MetricMetadata.Types.MetricType.Gauge,
    MetricFamilyName = MetricFamilyName
  };
}
