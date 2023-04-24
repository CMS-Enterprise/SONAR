using System;

namespace Cms.BatCave.Sonar.Extensions;

public static class DateTimeExtensions {
  /// <summary>
  ///   Returns a copy of the specified <see cref="DateTime" /> <paramref name="value" /> with the
  ///   precision reduced to milliseconds (nanoseconds and any smaller faction of time are truncated).
  /// </summary>
  public static DateTime TruncateNanoseconds(this DateTime value) {
    return new DateTime((value.Ticks / TimeSpan.TicksPerMillisecond) * TimeSpan.TicksPerMillisecond, value.Kind);
  }

  public static Double MillisSinceUnixEpoch(this DateTime value) {
    return value.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;
  }

  public static Double SecondsSinceUnixEpoch(this DateTime value) {
    return value.MillisSinceUnixEpoch() / 1000.0;
  }
}
