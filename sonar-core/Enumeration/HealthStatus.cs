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
  /// <summary>
  ///   Indicates that a service is currently in maintenance mode and its true status should be
  ///   ignored.
  /// </summary>
  /// <remarks>
  ///   A service should never truly be reported as having this status and it is not a valid value
  ///   for health check conditions.
  /// </remarks>
  Maintenance = -1,

  /// <summary>
  ///   Indicates that the status of a service could not be determined. This could be the result of
  ///   a health check failing do to an unexpected error, or a failure of the SONAR Agent to report
  ///   a status for the service, or one of the child services has the Unknown status.
  /// </summary>
  Unknown = 0,
  Online,
  AtRisk,
  Degraded,
  Offline
}
