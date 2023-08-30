using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Cms.BatCave.Sonar.Agent.Configuration;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Json.Path;
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
      // corresponding status is more severe than that based on status code.
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

      var responseJsonConditions =
        definition.Conditions.Where(c => (c.Type == HttpHealthCheckConditionType.HttpBodyJson));
      //If there are more conditions to be met reset status to unknown
      if (responseJsonConditions.Any()) {
        currCheck = HealthStatus.Unknown;
      }
      foreach (var conditionBody in responseJsonConditions) {
        var condition = (HttpBodyHealthCheckCondition)conditionBody;
        if (condition.Type == HttpHealthCheckConditionType.HttpBodyJson) {
          var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
          JsonNode rootNode = JsonNode.Parse(jsonString)!;

          var success = JsonPath.TryParse(condition.Path, out JsonPath? path);
          if (success) {
            var pathResult = path?.Evaluate(rootNode);
            var node = pathResult?.Matches?.FirstOrDefault();
            if ((node != null) && (node.Value != null)) {
              var rg = new Regex(condition.ValueRegex);
              if (rg.IsMatch(node.Value.ToString())) {
                currCheck = condition.Status;
                this._logger.LogDebug("{HealthCheck} path: {path} value: {value} status: {status}", healthCheck, condition.Path, node.Value, condition.Status);
              }
            } else {
              this._logger.LogError("{HealthCheck} no value at path {path}", healthCheck, path);
              currCheck = HealthStatus.Unknown;
            }
          } else {
            this._logger.LogError("{HealthCheck} Invalid path {path}", healthCheck, path);
            currCheck = HealthStatus.Unknown;
          }
        }
      }

      var responseXmlConditions =
        definition.Conditions.Where(c => (c.Type == HttpHealthCheckConditionType.HttpBodyXml));
      //If there are more conditions to be met reset status to unknown
      if (responseXmlConditions.Any()) {
        currCheck = HealthStatus.Unknown;
      }
      foreach (var conditionBody in responseXmlConditions) {
        var condition = (HttpBodyHealthCheckCondition)conditionBody;
        try {
          var xml = await response.Content.ReadAsStringAsync(cancellationToken);
          XmlDocument doc = new XmlDocument();
          doc.LoadXml(xml);
          XmlNode? xNode = doc.SelectSingleNode(condition.Path);
          if (xNode != null) {
            var rg = new Regex(condition.ValueRegex);
            if (rg.IsMatch(xNode.InnerText)) {
              this._logger.LogDebug("{HealthCheck} path: {path} value: {value} status: {status}", healthCheck, condition.Path, xNode.InnerText, condition.Status);
              currCheck = condition.Status;
            }
          } else {
            this._logger.LogError("{HealthCheck} No value at Path {path}", healthCheck, condition.Path);
            currCheck = HealthStatus.Unknown;
          }
        } catch (Exception ex) {
          this._logger.LogError("{HealthCheck} Invalid XML {message}", healthCheck, ex.Message);
          currCheck = HealthStatus.Unknown;
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
