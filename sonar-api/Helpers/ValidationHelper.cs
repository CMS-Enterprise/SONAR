using System;
using Cms.BatCave.Sonar.Exceptions;

namespace Cms.BatCave.Sonar.Helpers;

public class ValidationHelper {
  private const Int32 MaxSecondsInFuture = 10;
  public static void ValidateTimestamp(DateTime timestamp, String timestampName = "timestamp") {
    if (timestamp.Kind != DateTimeKind.Utc) {
      throw new BadRequestException(
        message: $"Invalid value for {timestampName}: non-utc timestamp",
        ProblemTypes.InvalidData
      );
    }

    if (timestamp.Subtract(DateTime.UtcNow).TotalSeconds > MaxSecondsInFuture) {
      throw new BadRequestException(
        message: $"Invalid value for {timestampName}: timestamp provided is too far in the future",
        ProblemTypes.InvalidData
      );
    }
  }

  public static void ValidateTimestampHasTimezone(DateTime timestamp, String timestampName = "timestamp") {
    if (timestamp.Kind is DateTimeKind.Unspecified) {
      throw new BadRequestException($"Invalid value for {timestampName}: time zone must be specified.");
    }
  }
}
