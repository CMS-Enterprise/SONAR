using System;
using System.Collections.Generic;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Alerting;

public class AlertingGlobalConfigurationGenerator {
  private const String SonarAlertmanagerSecretMountPath = "/sonar-config/secrets";
  private const String SmtpPasswordFileName = "smtp_password.txt";

  private readonly IOptions<GlobalAlertingConfiguration> _globalAlertingConfig;
  private readonly ILogger<AlertingGlobalConfigurationGenerator> _logger;

  public AlertingGlobalConfigurationGenerator(
    ILogger<AlertingGlobalConfigurationGenerator> logger,
    IOptions<GlobalAlertingConfiguration> globalAlertingConfig) {

    this._globalAlertingConfig = globalAlertingConfig;
    this._logger = logger;
  }

  internal (IDictionary<String, Object> GlobalConfig, IDictionary<String, String> SecretData)
    GenerateAlertmanagerConfigData() {

    var globalConfigData = new Dictionary<String, Object>();
    var alertmanagerSecretData = new Dictionary<String, String>();

    // Create global SMTP configuration

    if (this._globalAlertingConfig.Value.SmtpSettings != null) {

      var smtpConfig = this._globalAlertingConfig.Value.SmtpSettings!;

      globalConfigData["smtp_from"] = smtpConfig.Sender;
      globalConfigData["smtp_smarthost"] = $"{smtpConfig.Host}:{smtpConfig.Port}";

      if (smtpConfig.Port == 465) {
        // SMTP over port 465 implies a TLS session is started prior to any SMTP
        // commands are sent, whereas setting smtp_require_tls to true (which is
        // the default) causes Alertmanager to send the STARTTLS command, which
        // the SMTP server may not support and should not be required if using
        // port 465
        globalConfigData["smtp_require_tls"] = false;
      }

      if (smtpConfig.Username != null) {
        globalConfigData["smtp_auth_username"] = smtpConfig.Username;
      }

      if (smtpConfig.Password != null) {
        // Store the SMTP password in a Kubernetes Secret and point the
        // Alertmanager configuration at the location where that secret is
        // mounted;
        alertmanagerSecretData[SmtpPasswordFileName] = smtpConfig.Password;

        globalConfigData["smtp_auth_password_file"] =
          $"{SonarAlertmanagerSecretMountPath}/{SmtpPasswordFileName}";
      }
    } else {
      this._logger.LogWarning(
        "Kubernetes alerting configuration is enabled, but no SMTP settings have been specified"
      );
    }

    return (globalConfigData, alertmanagerSecretData);
  }
}
