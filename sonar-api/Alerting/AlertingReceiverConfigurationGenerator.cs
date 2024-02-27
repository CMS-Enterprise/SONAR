using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Configuration;
using Cms.BatCave.Sonar.Helpers;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Alerting;

public class AlertingReceiverConfigurationGenerator {
  public const String DefaultReceiverName = "default-receiver";
  public const String NullReceiverName = "null-receiver";

  private const String AlertmanagerEmailTextTemplate = "{{ template \"email.sonar-alerts.text\" . }}";
  private const String AlertmanagerEmailHtmlTemplate = "{{ template \"email.sonar-alerts.html\" . }}";

  private readonly IOptions<GlobalAlertingConfiguration> _globalAlertingConfig;
  private readonly EnvironmentDataHelper _environmentDataHelper;
  private readonly TenantDataHelper _tenantDataHelper;
  private readonly AlertingDataHelper _alertingDataHelper;

  public AlertingReceiverConfigurationGenerator(
    IOptions<GlobalAlertingConfiguration> globalAlertingConfig,
    EnvironmentDataHelper environmentDataHelper,
    TenantDataHelper tenantDataHelper,
    AlertingDataHelper alertingDataHelper) {

    this._globalAlertingConfig = globalAlertingConfig;
    this._environmentDataHelper = environmentDataHelper;
    this._tenantDataHelper = tenantDataHelper;
    this._alertingDataHelper = alertingDataHelper;
  }

  internal async Task<List<AlertmanagerReceiverConfiguration>> GenerateAlertmanagerReceiverConfiguration(
    CancellationToken cancellationToken) {

    // (Re)Generate alert receivers configuration for all tenants
    var listOfAlertReceivers = new List<AlertmanagerReceiverConfiguration>();

    var existingEnvironments =
      await this._environmentDataHelper.FetchAllExistingEnvAsync(cancellationToken);

    foreach (var environment in existingEnvironments) {

      var tenantsForEnvironment =
        await this._tenantDataHelper.ListTenantsForEnvironment(
          environment.Id,
          cancellationToken);

      foreach (var tenant in tenantsForEnvironment) {
        var alertReceiversForTenant =
          await this._alertingDataHelper.FetchAlertReceiversAsync(
            tenant.Id,
            cancellationToken);

        foreach (var receiver in alertReceiversForTenant) {
          var receiverName = $"{environment.Name}_{tenant.Name}_{receiver.Name}";
          var receiverOptions = receiver.DeserializeOptions();
          if (receiverOptions is AlertReceiverOptionsEmail emailReceiverOptions) {
            listOfAlertReceivers.Add(new AlertmanagerReceiverConfiguration(
              receiverName,
              ImmutableList<AlertmanagerReceiverEmailConfig>.Empty
                .Add(new AlertmanagerReceiverEmailConfig(
                  To: emailReceiverOptions.Address,
                  Text: AlertmanagerEmailTextTemplate,
                  Html: AlertmanagerEmailHtmlTemplate,
                  TlsConfig: new AlertmanagerTlsConfig()
                  )))
            );
          } else {
            throw new NotSupportedException(
              $"The {nameof(AlertReceiverOptions)} type {receiverOptions.GetType().Name} is not supported."
            );
          }
        }
      }
    }

    listOfAlertReceivers.Add(new AlertmanagerReceiverConfiguration(
      DefaultReceiverName,
      ImmutableList<AlertmanagerReceiverEmailConfig>.Empty
        .Add(new AlertmanagerReceiverEmailConfig(
          To: this._globalAlertingConfig.Value.DefaultReceiverEmail,
          Text: AlertmanagerEmailTextTemplate,
          Html: AlertmanagerEmailHtmlTemplate,
          TlsConfig: new AlertmanagerTlsConfig()
          )))
    );

    listOfAlertReceivers.Add(new AlertmanagerReceiverConfiguration(NullReceiverName));

    return listOfAlertReceivers;
  }
}
