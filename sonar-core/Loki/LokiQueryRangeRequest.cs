using System;
using Cms.BatCave.Sonar.Query;

namespace Cms.BatCave.Sonar.Loki;

public record LokiQueryRangeRequest(
  String Query,
  Int32 Limit,
  DateTime Start,
  DateTime End,
  TimeSpan Step,
  Direction Direction
  );

