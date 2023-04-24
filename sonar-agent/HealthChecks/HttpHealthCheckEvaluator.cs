using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Agent.HealthChecks;

/// <summary>
/// A <see cref="IHealthCheckEvaluator{TDefinition}" /> implementation for HTTP based health checks.
/// </summary>
public class HttpHealthCheckEvaluator : IHealthCheckEvaluator<HttpHealthCheckDefinition> {
  private static readonly HttpHealthCheckCondition DefaultStatusCodeCondition = new StatusCodeCondition(
    new UInt16[] { 200, 204 },
    HealthStatus.Online
  );

  private readonly IOptions<AgentConfiguration> _agentConfig;
  private readonly ILogger<HttpHealthCheckEvaluator> _logger;

  public HttpHealthCheckEvaluator(
    IOptions<AgentConfiguration> agentConfig,
    ILogger<HttpHealthCheckEvaluator> logger) {

    this._agentConfig = agentConfig;
    this._logger = logger;
  }

  public async Task<HealthStatus> EvaluateHealthCheckAsync(
    HealthCheckIdentifier healthCheck,
    HttpHealthCheckDefinition definition,
    CancellationToken cancellationToken = default) {

    try {
      using var handler = new HttpClientHandler {
        AllowAutoRedirect = definition.FollowRedirects != false
      };

      if (definition.SkipCertificateValidation == true) {
        handler.ServerCertificateCustomValidationCallback = (_, _, _, errors) => {
          if (errors != SslPolicyErrors.None) {
            this._logger.LogDebug(
              "Ignoring SSL Certificate Validation Errors ({CertificateErrors}) for Request {Url}",
              String.Join(separator: ", ", Enum.GetValues<SslPolicyErrors>().Where(v => v != SslPolicyErrors.None && errors.HasFlag(v))),
              definition.Url
            );
          }

          return true;
        };
      }

      using var client = new HttpClient(handler);
      client.Timeout = TimeSpan.FromSeconds(this._agentConfig.Value.AgentInterval);

      if (definition.AuthorizationHeader != null) {
        if (AuthenticationHeaderValue.TryParse(definition.AuthorizationHeader, out var auth)) {
          client.DefaultRequestHeaders.Authorization = auth;
        } else {
          this._logger.LogWarning(
            "Invalid AuthorizationHeader value for health check {HealthCheck}",
            healthCheck
          );
        }
      }

      // Start out offline
      var currCheck = HealthStatus.Offline;

      // Send request to url specified in definition, calculate duration of request
      var now = DateTime.Now;
      var response = await client.GetAsync(definition.Url, cancellationToken);
      var duration = DateTime.Now - now;

      // Passed error handling, get status code from response.
      var statusCode = (UInt16)response.StatusCode;

      // Evaluate all status code conditions first
      var statusCodeConditions =
        definition.Conditions
          .Where(c => c.Type == HttpHealthCheckConditionType.HttpStatusCode)
          // If no HttpStatusCode conditions are specified, require the status code to be 200/204
          .DefaultIfEmpty(HttpHealthCheckEvaluator.DefaultStatusCodeCondition);
      var conditionMet = false;
      foreach (var condition in statusCodeConditions) {
        var statusCondition = (StatusCodeCondition)condition;
        if (statusCondition.StatusCodes.Contains(statusCode)) {
          this._logger.LogDebug(
            "Status code condition {StatusCode} => {ServiceHealth} met for health check {HealthCheck}",
            statusCode,
            statusCondition.Status,
            healthCheck
          );

          currCheck = statusCondition.Status;
          conditionMet = true;
        }
      }

      // If no status code conditions are met, the status will be Offline.
      if (!conditionMet) {
        this._logger.LogDebug(
          "No status code conditions matched {StatusCode} for health check {HealthCheck}",
          statusCode,
          healthCheck
        );

        return HealthStatus.Offline;
      }

      // Evaluate response time conditions. These conditions will only have an effect if the
      // corresponding status is more sever than that based on status code.
      var responseTimeConditions =
        definition.Conditions.Where(c => c.Type == HttpHealthCheckConditionType.HttpResponseTime);
      foreach (var condition in responseTimeConditions) {
        var responseCondition = (ResponseTimeCondition)condition;
        if ((duration > responseCondition.ResponseTime) && (responseCondition.Status > currCheck)) {
          this._logger.LogDebug(
            "Request duration exceeded threshold ({Threshold}) for health check {HealthCheck}",
            responseCondition.ResponseTime,
            healthCheck
          );

          currCheck = responseCondition.Status;
        }
      }

      return currCheck;
    } catch (HttpRequestException e) {
      // Request failed, set currCheck to offline and return.
      this._logger.LogDebug(
        e,
        "Request to {Url} failed for health check {HealthCheck}: {Message}",
        definition.Url,
        healthCheck,
        e.Message
      );

      return HealthStatus.Offline;
    } catch (Exception e) when (e is InvalidOperationException or UriFormatException) {
      // Error with requestURI, this means the configuration is invalid, log and return unknown status.
      this._logger.LogWarning(
        "Invalid Health Check URL: {Url} for health check {HealthCheck}",
        definition.Url,
        healthCheck
      );

      return HealthStatus.Unknown;
    } catch (OperationCanceledException) {
      if (cancellationToken.IsCancellationRequested) {
        // Task cancelled, raise exception
        throw;
      } else {
        // Http Timeout. Consider the service offline.
        this._logger.LogDebug(
          "Request to {Url} timed out for health check {HealthCheck}",
          definition.Url,
          healthCheck
        );

        return HealthStatus.Offline;
      }
    }
  }
}
