using System;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Alerting;

public record PrometheusAlertingGroup(
  String Name,
  ImmutableList<PrometheusAlertingRule> Rules);
