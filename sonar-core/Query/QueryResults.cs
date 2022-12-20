using System;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Query;

public record QueryResults(
  QueryResultType ResultType,
  IImmutableList<ResultData> Result,
  IImmutableList<String>? Statistics);
