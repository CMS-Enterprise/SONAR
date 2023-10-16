namespace Cms.BatCave.Sonar.Exceptions;

public enum InvalidConfigurationErrorType {
  TopLevelNull,
  InvalidJson,
  IncompatibleHealthCheckType,
  DataValidationError,
  IncompatibleHttpHealthCheckConditionType
}
