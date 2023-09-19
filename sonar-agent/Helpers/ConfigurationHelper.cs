using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
  private readonly ErrorReportsHelper _errorReportsHelper;
  public ConfigurationHelper(
    IServiceConfigSource serviceConfigSource,
    Func<(IDisposable, ISonarClient)> sonarClientFactory,
    ILogger<ConfigurationHelper> logger,
    ErrorReportsHelper errorReportsHelper) {

    this._serviceConfigSource = serviceConfigSource;
    this._sonarClientFactory = sonarClientFactory;
    this._logger = logger;
    this._errorReportsHelper = errorReportsHelper;
  }

  /// <summary>
  ///   Load Service Configuration from both local files and the Kubernetes API according to command line
  ///   options.
  /// </summary>
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

    await foreach (var tenant in this._serviceConfigSource.GetTenantsAsync(cancellationToken)) {
      var layers =
        await this._serviceConfigSource.GetConfigurationLayersAsync(tenant, cancellationToken)
          .ToListAsync(cancellationToken);

      if (layers.Count > 0) {
        var mergedConfiguration = layers.Aggregate(ServiceConfigMerger.MergeConfigurations);

        try {
          ServiceConfigValidator.ValidateServiceConfig(mergedConfiguration);
          configurationByTenant.Add(tenant, mergedConfiguration);
        } catch (InvalidConfigurationException e) {
          this._logger.LogError(e,
            message: "Tenant service configuration is invalid, skipping initial load: {tenant}.",
            tenant);
        }
      } else {
        configurationByTenant.Add(tenant, ServiceHierarchyConfiguration.Empty);
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