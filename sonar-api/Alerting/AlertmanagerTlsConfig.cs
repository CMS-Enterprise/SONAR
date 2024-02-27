using System;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Alerting;

/// <summary>
/// Configuration class that corresponds with Alertmanager alert receiver tls configuration; see Alertmanager docs
/// for <a href="https://prometheus.io/docs/alerting/latest/configuration/#tls_config">tls_config</a> for more
/// info on the fields we include in the Alertmanager configuration we generate.
/// </summary>

internal record AlertmanagerTlsConfig(
  [property:JsonPropertyName("insecure_skip_verify")]
    Boolean InsecureSkipVerify = true
);

