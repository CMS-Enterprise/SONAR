using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Cms.BatCave.Sonar.Alerting;

public class PrometheusTimeSpanConverter : JsonConverter<TimeSpan> {
  private static readonly Regex PrometheusDurationRegex =
    new(
      @"^((?<years>[0-9]+)y)?
         ((?<weeks>[0-9]+)w)?
         ((?<days>[0-9]+)d)?
         ((?<hours>[0-9]+)h)?
         ((?<minutes>[0-9]+)m)?
         ((?<seconds>[0-9]+)s)?
         ((?<milliseconds>[0-9]+)ms)?$",
      RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

  public override TimeSpan Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options) {

    var str = reader.GetString();
    if (!String.IsNullOrEmpty(str)) {
      var match = PrometheusDurationRegex.Match(str);
      if (match.Success) {
        return new TimeSpan(
          (match.Groups["years"].Success ? Int32.Parse(match.Groups["years"].Value) * 365 : 0) +
          (match.Groups["weeks"].Success ? Int32.Parse(match.Groups["weeks"].Value) * 7 : 0) +
          (match.Groups["days"].Success ? Int32.Parse(match.Groups["days"].Value) : 0),
          match.Groups["hours"].Success ? Int32.Parse(match.Groups["hours"].Value) : 0,
          match.Groups["minutes"].Success ? Int32.Parse(match.Groups["minutes"].Value) : 0,
          match.Groups["seconds"].Success ? Int32.Parse(match.Groups["seconds"].Value) : 0,
          match.Groups["milliseconds"].Success ? Int32.Parse(match.Groups["milliseconds"].Value) : 0
        );
      } else {
        throw new JsonException(
          $"Unable to parse value as a Prometheus duration ([1y][2w][3d][4h][5m][6s][7ms]): {str}");
      }
    } else {
      throw new JsonException("Expected to find a non-empty string value representing a TimeSpan.");
    }
  }

  public override void Write(
    Utf8JsonWriter writer,
    TimeSpan value,
    JsonSerializerOptions options) {

    if (value < TimeSpan.Zero) {
      throw new ArgumentOutOfRangeException(nameof(value));
    }

    if (value == TimeSpan.Zero) {
      writer.WriteStringValue("0s");
      return;
    }

    var sb = new StringBuilder();
    var days = value.Days;
    if (days >= 365) {
      sb.AppendFormat("{0:D}y", days / 365);
      days = days % 365;
    }

    if (days >= 7) {
      sb.AppendFormat("{0:D}w", days / 7);
      days = days % 7;
    }

    if (days >= 1) {
      sb.AppendFormat("{0:D}d", days);
    }

    if (value.Hours > 0) {
      sb.AppendFormat("{0:D}h", value.Hours);
    }

    if (value.Minutes > 0) {
      sb.AppendFormat("{0:D}m", value.Minutes);
    }

    if (value.Seconds > 0) {
      sb.AppendFormat("{0:D}s", value.Seconds);
    }

    if (value.Milliseconds > 0) {
      sb.AppendFormat("{0:D}ms", value.Milliseconds);
    }

    writer.WriteStringValue(sb.ToString());
  }
}
