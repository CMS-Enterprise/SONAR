using System;

namespace Cms.BatCave.Sonar.Configuration;

public record GlobalAlertingConfiguration(
  String DefaultReceiverEmail,
  SmtpConfiguration? SmtpSettings = null,

  // How often we synchronize our Alertmanager configuration and Prometheus alerting rules
  // ConfigMaps with the latest alerting configuration in our DB (in a background thread).
  Int32 ConfigSyncIntervalSeconds = 300
);
