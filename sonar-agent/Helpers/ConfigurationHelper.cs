using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Helpers;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent;

public class ConfigurationHelper {
  private readonly IServiceConfigSource _serviceConfigSource;
  private readonly Func<(IDisposable, ISonarClient)> _sonarClientFactory;
  private readonly ILogger<ConfigurationHelper> _logger;
  private readonly IErrorReportsHelper _errorReportsHelper;
  public ConfigurationHelper(
    IServiceConfigSource serviceConfigSource,
    Func<(IDisposable, ISonarClient)> sonarClientFactory,
    ILogger<ConfigurationHelper> logger,
    IErrorReportsHelper errorReportsHelper) {

    this._serviceConfigSource = serviceConfigSource;
    this._sonarClientFactory = sonarClientFactory;
    this._logger = logger;
    this._errorReportsHelper = errorReportsHelper;
  }

  /// <summary>
  ///   Load Service Configuration from both local files and the Kubernetes API according to command line
  ///   options.
  /// </summary>
  /// <param name="environment">The name of the environment.</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>
  ///   A dictionary of tenant names to <see cref="ServiceHierarchyConfiguration" /> for that tenant.
  /// </returns>
  /// <exception cref="ArgumentException">
  ///   Thrown when no tenant configuration is found.
  /// </exception>
  public async Task<IDictionary<String, ServiceHierarchyConfiguration>> LoadAndValidateJsonServiceConfigAsync(
    String environment,
    CancellationToken cancellationToken) {

    var configurationByTenant =
      new Dictionary<String, ServiceHierarchyConfiguration>(StringComparer.OrdinalIgnoreCase);

    // Check for Deserialization and Merging
    await foreach (var tenant in this._serviceConfigSource.GetTenantsAsync(cancellationToken)) {
      ServiceHierarchyConfiguration? mergedConfiguration = null;

      try {
        var layers =
          await this._serviceConfigSource.GetConfigurationLayersAsync(tenant, cancellationToken)
            .ToListAsync(cancellationToken);

        if (layers.Count > 0) {
          mergedConfiguration = layers.Aggregate(ServiceConfigMerger.MergeConfigurations);
          ServiceConfigValidator.ValidateServiceConfig(mergedConfiguration);
          configurationByTenant.Add(tenant, mergedConfiguration);
        } else {
          configurationByTenant.Add(tenant, ServiceHierarchyConfiguration.Empty);
        }
      } catch (InvalidConfigurationException cfgEx) {
        ErrorReportDetails errorReport;

        try {
          String? config = null;
          if (mergedConfiguration != null) {
            config = JsonSerializer.Serialize(mergedConfiguration);
          } else {
            config = cfgEx.RawConfig;
          }

          if ((cfgEx.ErrorType == InvalidConfigurationErrorType.InvalidJson) ||
            (cfgEx.ErrorType == InvalidConfigurationErrorType.TopLevelNull)) {
            this._logger.LogError(cfgEx,
              message:
              "An error occurred reading or deserializing service configuration, skipping initial load: {tenant}.",
              tenant);

            // Create Error Report for Deserialization and Merging
            errorReport = new ErrorReportDetails(
              timestamp: DateTime.UtcNow,
              tenant: tenant,
              service: null,
              healthCheckName: null,
              level: AgentErrorLevel.Error,
              type: AgentErrorType.Deserialization,
              message: cfgEx.Message,
              configuration: config,
              stackTrace: cfgEx.StackTrace);
          } else {
            this._logger.LogError(cfgEx,
              message: "Tenant service configuration is invalid, skipping initial load: {tenant}.",
              tenant);

            var invalidConfigErrorMessage = cfgEx.ReadValidationResults();
            this._logger.LogError(cfgEx, invalidConfigErrorMessage);

            // Create Error Report for Validation
            errorReport = new ErrorReportDetails(
              timestamp: DateTime.UtcNow,
              tenant: tenant,
              service: null,
              healthCheckName: null,
              level: AgentErrorLevel.Error,
              type: AgentErrorType.Validation,
              message: invalidConfigErrorMessage,
              configuration: config,
              stackTrace: cfgEx.StackTrace);
          }
          await this._errorReportsHelper.CreateErrorReport(environment, errorReport, cancellationToken);

        } catch (NotSupportedException nse) {
          // Error upon serializing ServiceHierarchyConfig due to invalid HealthCheckType
          errorReport = new ErrorReportDetails(
            timestamp: DateTime.UtcNow,
            tenant: tenant,
            service: null,
            healthCheckName: null,
            level: AgentErrorLevel.Error,
            type: AgentErrorType.Validation,
            message: nse.Message,
            configuration: null,
            stackTrace: nse.StackTrace);
          await this._errorReportsHelper.CreateErrorReport(environment, errorReport, cancellationToken);
        }
      }
    }

    return configurationByTenant;
  }

  /// <summary>
  ///   Save the specified service configuration to the SONAR API. This configuration may be for a new or
  ///   existing tenant.
  /// </summary>
  public async Task ConfigureServicesAsync(
    String environment,
    IDictionary<String, ServiceHierarchyConfiguration> tenantServiceDictionary,
    CancellationToken token) {

    // SONAR client
    // TODO(BATAPI-208): make this a dependency injected via the ConfigurationHelper constructor
    // using var http = new HttpClient();
    // var client = new SonarClient(this._configRoot, this._apiConfig.Value.BaseUrl, http);

    var (conn, client) = this._sonarClientFactory();
    try {
      foreach (var tenantServices in tenantServiceDictionary) {
        var tenant = tenantServices.Key;
        var servicesHierarchy = tenantServices.Value;
        try {
          // Set up service configuration for specified environment and tenant
          await client.CreateTenantAsync(
            environment, tenant, servicesHierarchy, token);
        } catch (ApiException requestException) {
          if (requestException.StatusCode == 409) {
            // Updating service configuration for existing environment and tenant
            await client.UpdateTenantAsync(
              environment, tenant, servicesHierarchy, token);
          } else {
            throw;
          }
        }
      }
    } finally {
      conn.Dispose();
    }
  }

  public async Task DeleteServicesAsync(
    String environment,
    String tenant,
    CancellationToken token) {

    // SONAR client

    var (conn, client) = this._sonarClientFactory();
    try {
      await client.DeleteTenantAsync(environment, tenant, token);
    } catch (ApiException e) {
      this._logger.LogError(
        e,
        "Failed to delete tenant service configuration in SONAR API, Code: {StatusCode}, Message: {Message}",
        e.StatusCode,
        e.Message
      );

      // create error report
      await this._errorReportsHelper.CreateErrorReport(
        environment,
        new ErrorReportDetails(
          DateTime.UtcNow,
          tenant,
          null,
          null,
          AgentErrorLevel.Error,
          AgentErrorType.SaveConfiguration,
          e.Message,
          null,
          null),
        token);
    } finally {
      conn.Dispose();
    }
  }
}
