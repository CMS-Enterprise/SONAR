using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using CommandLine;
using Microsoft.EntityFrameworkCore;
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
  /// <param name="environment"></param>
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

    var (conn, client) = this._sonarClientFactory();
    var configurationByTenant =
      new Dictionary<String, ServiceHierarchyConfiguration>(StringComparer.OrdinalIgnoreCase);

    //Check for Deserialization and Merging
    try {
      await foreach (var tenant in this._serviceConfigSource.GetTenantsAsync(cancellationToken)) {
        ServiceHierarchyConfiguration? mergedConfiguration = null;
        try {
          var layers =
            await this._serviceConfigSource.GetConfigurationLayersAsync(tenant, cancellationToken)
              .ToListAsync(cancellationToken);

          if (layers.Count > 0) {
            mergedConfiguration = layers.Aggregate(ServiceConfigMerger.MergeConfigurations);

            try {
              ServiceConfigValidator.ValidateServiceConfig(mergedConfiguration);
              configurationByTenant.Add(tenant, mergedConfiguration);

            } catch (InvalidConfigurationException e) {
              this._logger.LogError(e,
                message: "Tenant service configuration is invalid, skipping initial load: {tenant}.",
                tenant);

              //Create Error Report for Validation
              var data = (List<ValidationResult>)e.Data["errors"]!;
              var errorReport = new ErrorReportDetails(
                timestamp: DateTime.UtcNow,
                tenant: tenant,
                service: null,
                healthCheckName: null,
                level: AgentErrorLevel.Error,
                type: AgentErrorType.Validation,
                message: data
                  .Select(em => em.ErrorMessage)
                  .Aggregate("", (current, next) => current + ' ' + next),
                configuration: null,
                stackTrace: e.StackTrace
              );
              await client.CreateErrorReportAsync(environment, errorReport, cancellationToken);
            }
          } else {
            configurationByTenant.Add(tenant, ServiceHierarchyConfiguration.Empty);
          }
        } catch (Exception e) when (e is InvalidConfigurationException or ServiceConfigSourceException) {
          this._logger.LogError(e,
            message: "An error occurred reading or deserializing service configuration, skipping initial load: {tenant}.",
            tenant);

          String? config = null;
          if (mergedConfiguration != null) {
            config = "serialize the config object";
          } else if (e is ServiceConfigSourceException svcConfigEx) {
            config = svcConfigEx.RawConfig;
          }
          // Create Error Report for Deserialization and Merging
          var errorReport = new ErrorReportDetails(
            timestamp: DateTime.UtcNow,
            tenant: tenant,
            service: null,
            healthCheckName: null,
            level: AgentErrorLevel.Error,
            type: AgentErrorType.Deserialization,
            message: e.Message,
            configuration: config,
            stackTrace: e.StackTrace
          );
          await client.CreateErrorReportAsync(environment, errorReport, cancellationToken);
        }
      }
    } finally {
      conn.Dispose();
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
