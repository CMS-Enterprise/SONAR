using System;
using Cms.BatCave.Sonar.Exceptions;

namespace Cms.BatCave.Sonar.Helpers;

public class ValidationHelper {
  private const Int32 MaxSecondsInFuture = 10;
  public static void ValidateTimestamp(DateTime timestamp) {
    if (timestamp.Kind != DateTimeKind.Utc) {
      throw new BadRequestException(
        message: "Invalid sample timestamp: non-utc timestamp",
        ProblemTypes.InvalidData
      );
    }

    if (timestamp.Subtract(DateTime.UtcNow).TotalSeconds > MaxSecondsInFuture) {
      throw new BadRequestException(
        message: "Invalid sample timestamp: timestamp provided is too far in the future",
        ProblemTypes.InvalidData
      );
    }
  }
}
