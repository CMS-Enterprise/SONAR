namespace Cms.BatCave.Sonar.Enumeration;

/// <summary>
///   An enumeration representing the status of a service or health check.
/// </summary>
/// <remarks>
///   The greater the numeric value of the enum entry, the more severe the status. Code should not
///   depend on the use of specific numeric values aside from Online being equal to <c>1</c>, since
///   additional entries may be added in the future.
/// </remarks>
public enum HealthStatus {
  Unknown = 0,
  Online,
  AtRisk,
  Degraded,
  Offline
}
