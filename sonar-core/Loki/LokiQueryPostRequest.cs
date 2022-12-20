using System;
using Cms.BatCave.Sonar.Query;

namespace Cms.BatCave.Sonar.Loki;


public record LokiQueryPostRequest(
  String Query,
  Int32 Limit,
  DateTime Timestamp,
  Direction Direction
  );
