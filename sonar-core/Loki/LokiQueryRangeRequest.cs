using System;

namespace Cms.BatCave.Sonar.Loki;

public record LokiQueryRangeRequest(
  String Query,
  Int32 Limit,
  DateTime Start,
  DateTime End,
  TimeSpan Step,
  Direction Direction
  );

