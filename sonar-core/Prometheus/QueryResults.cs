using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Prometheus;

public record QueryResults(
  QueryResultType ResultType,
  IImmutableList<ResultData> Result);
