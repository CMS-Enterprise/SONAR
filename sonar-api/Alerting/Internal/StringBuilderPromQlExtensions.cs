using System;
using System.Text;

namespace Cms.BatCave.Sonar.Alerting.Internal;

internal static class StringBuilderPromQlExtensions {
  internal static StringBuilder PromQlMin(this StringBuilder sb, Action buildExpr) {
    sb.Append("min(");
    buildExpr();
    sb.Append(")");
    return sb;
  }

  internal static StringBuilder PromQlMax(this StringBuilder sb, Action buildExpr) {
    sb.Append("max(");
    buildExpr();
    sb.Append(")");
    return sb;
  }

  internal static StringBuilder PromQlBy(this StringBuilder sb, params String[] labels) {
    sb.Append(" by (");
    sb.AppendJoin(", ", labels);
    sb.Append(')');
    return sb;
  }

  internal static StringBuilder PromQlLabelReplace(
    this StringBuilder sb,
    Action buildExpr,
    String dest,
    String replacement,
    String original,
    String match) {

    sb.Append("label_replace(");
    buildExpr();
    sb.Append(", \"");
    sb.AppendJoin("\", \"", dest, replacement, original, match);
    sb.Append("\")");
    return sb;
  }

  internal static StringBuilder PromQlSelector(
    this StringBuilder sb,
    String metric,
    params PromQlLabelFilter[] selectors) {

    sb.Append(metric);
    sb.Append('{');
    sb.AppendJoin<PromQlLabelFilter>(", ", selectors);
    sb.Append('}');
    return sb;
  }
}
