using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Alerting;

public record PrometheusAlertingConfiguration(ImmutableList<PrometheusAlertingGroup> Groups);
