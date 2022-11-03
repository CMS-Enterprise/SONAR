using System;

namespace Cms.BatCave.Sonar.Prometheus;

public record QueryRangePostRequest(
  String Query,
  DateTime Start,
  DateTime End,
  TimeSpan Step,
  TimeSpan Timeout);
