using System;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Alerting;

/// <summary>
/// Configuration class that corresponds with Alertmanager alert receiver email configuration; see Alertmanager docs
/// for <a href="https://prometheus.io/docs/alerting/latest/configuration/#email_config">email_config</a> for more
/// info on the fields we include in the Alertmanager configuration we generate.
/// </summary>
internal record AlertmanagerReceiverEmailConfig(
  String To,
  String Text,
  String Html,
  [property:JsonPropertyName("tls_config")]
  AlertmanagerTlsConfig TlsConfig
  );
