using System;

namespace Cms.BatCave.Sonar.Query;

public record QueryRangePostRequest(
  String Query,
  DateTime Start,
  DateTime End,
  TimeSpan Step,
  TimeSpan? Timeout);
