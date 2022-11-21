using System;

namespace Cms.BatCave.Sonar;

public static class ProblemTypes {
  /// <summary>
  ///   The specified data is not consistent with the configuration.
  /// </summary>
  /// <remarks>
  ///   This problem type is used when a payload passes basic JSON validation, but is inconsistent in
  ///   some way with the configuration for the resource being manipulated.
  /// </remarks>
  public const String InconsistentData = "sonar:InconsistentData";

  /// <summary>
  ///   The specified data is not violates some constraint.
  /// </summary>
  /// <remarks>
  ///   This problem type is used when a payload passes basic JSON validation, but violates some more
  ///   advanced constraint.
  /// </remarks>
  public const String InvalidData = "sonar:InvalidData";

  /// <summary>
  ///   The specified configuration violates some constraint.
  /// </summary>
  /// <remarks>
  ///   This problem type is used when a configuration payload passes basic JSON validation, but violates
  ///   some more advanced constraint such as containing an internal reference to a resource that does
  ///   not exist.
  /// </remarks>
  public const String InvalidConfiguration = "sonar:InvalidConfiguration";
}
