using System;
using System.Collections.Generic;
using Cms.BatCave.Sonar.Extensions;

namespace Cms.BatCave.Sonar.Alerting.Internal;

public record PromQlLabelFilter(String Label, PromQlOperator Operator, String Value) {
  private static readonly IDictionary<PromQlOperator, String> Operators = new Dictionary<PromQlOperator, String> {
    { PromQlOperator.Equal, "=" },
    { PromQlOperator.NotEqual, "!=" },
    { PromQlOperator.RegexMatch, "=~" },
    { PromQlOperator.NotRegexMatch, "!~" },
  };

  public override String ToString() {
    return $"{this.Label}{Operators[this.Operator]}\"{this.Value.Escape()}\"";
  }
}
