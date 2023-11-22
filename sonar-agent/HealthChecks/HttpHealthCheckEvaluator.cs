using System;
using System.Collections.Generic;
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
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.Helpers;
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
  private readonly Func<HttpHealthCheckDefinition, HttpMessageHandler> _httpMsgHandlerFactory;

  public HttpHealthCheckEvaluator(
    IOptions<AgentConfiguration> agentConfig,
    ILogger<HttpHealthCheckEvaluator> logger) {

    this._agentConfig = agentConfig;
    this._logger = logger;

    this._httpMsgHandlerFactory = this.CreateHttpClientHandler;
  }

  public HttpHealthCheckEvaluator(
    IOptions<AgentConfiguration> agentConfig,
    ILogger<HttpHealthCheckEvaluator> logger,
    Func<HttpHealthCheckDefinition, HttpMessageHandler> httpMsgHandlerFactory) {

    this._agentConfig = agentConfig;
    this._logger = logger;
    this._httpMsgHandlerFactory = httpMsgHandlerFactory;
  }

  public async Task<HealthStatus> EvaluateHealthCheckAsync(
    HealthCheckIdentifier healthCheck,
    HttpHealthCheckDefinition definition,
    CancellationToken cancellationToken = default) {

    try {
      var handler = this._httpMsgHandlerFactory(definition);

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

      var responseJsonConditions =
        definition.Conditions
          .Where(c => c.Type == HttpHealthCheckConditionType.HttpBodyJson)
          .Cast<HttpBodyHealthCheckCondition>()
          .ToList();

      var responseXmlConditions =
        definition.Conditions.Where(c => c.Type == HttpHealthCheckConditionType.HttpBodyXml)
          .Cast<HttpBodyHealthCheckCondition>()
          .ToList();
      if ((responseJsonConditions.Count > 0) && (responseXmlConditions.Count > 0)) {
        this._logger.LogError(
          "Unable to evaluate conditions for {HealthCheck}: both XML and JSON response body conditions are specified",
          healthCheck
        );
        return HealthStatus.Unknown;
      } else if (responseJsonConditions.Count > 0) {
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      } else if (responseXmlConditions.Count > 0) {
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
      }

      // Start out offline
      var currCheck = HealthStatus.Unknown;

      // Send request to url specified in definition, calculate duration of request
      var now = DateTime.Now;
      var response = await client.GetAsync(definition.Url, cancellationToken);
      var duration = DateTime.Now - now;

      // Passed error handling, get status code from response.
      var statusCode = (UInt16)response.StatusCode;

      // Evaluate all status code conditions first
      // If no HttpStatusCode conditions are specified, require the status code to be 200/204
      var statusCodeConditions =
        definition.Conditions
          .Where(c => c.Type == HttpHealthCheckConditionType.HttpStatusCode)
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
          break;
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

      //Get content from response.
      var contentString = await response.Content.ReadAsStringAsync(cancellationToken);

      //If there are conditions to be checked, initialize currentCondition to unKnown
      //If no conditions are met - the status will remain unknown.
      if ((responseJsonConditions.Count > 0) || (responseXmlConditions.Count > 0)) {
        currCheck = HealthStatus.Unknown;
      }

      //Evaluate JsonConditions
      foreach (var condition in responseJsonConditions) {
        currCheck = EvaluateCondition(healthCheck, currCheck, HttpBodyType.Json, contentString, condition);
      }

      //Evaluate xml conditions
      foreach (var condition in responseXmlConditions) {
        currCheck = EvaluateCondition(healthCheck, currCheck, HttpBodyType.Xml, contentString, condition);
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

  private HttpClientHandler CreateHttpClientHandler(HttpHealthCheckDefinition definition) {
    using var handler = new HttpClientHandler {
      AllowAutoRedirect = definition.FollowRedirects != false
    };

    if (definition.SkipCertificateValidation == true) {
      handler.ServerCertificateCustomValidationCallback = (_, _, _, errors) => {
        if (errors != SslPolicyErrors.None) {
          this._logger.LogDebug(
            "Ignoring SSL Certificate Validation Errors ({CertificateErrors}) for Request {Url}",
            String.Join(separator: ", ",
              Enum.GetValues<SslPolicyErrors>().Where(v => v != SslPolicyErrors.None && errors.HasFlag(v))),
            definition.Url
          );
        }

        return true;
      };
    }

    return handler;
  }

  public HealthStatus EvaluateCondition(
    HealthCheckIdentifier healthCheckIdentifier,
    HealthStatus currCheck,
    HttpBodyType httpBodyType,
    String contentString,
    HttpBodyHealthCheckCondition condition) {

    var result = String.Empty;
    if (currCheck == HealthStatus.Offline) { return currCheck; }
    try {
      result = DocumentValueExtractor.GetStringValue(httpBodyType, contentString, condition.Path);
    } catch (DocumentValueExtractorException dve) {
      this._logger.LogError("{HealthCheck}  {message}", healthCheckIdentifier, dve.Message);
    } catch (Exception e) {
      this._logger.LogError("{HealthCheck}  {message}", healthCheckIdentifier, e.Message);
    }

    var regex = new Regex(condition.Value);
    if (!String.IsNullOrEmpty(result) && regex.IsMatch(result)) {
      if (condition.Status > currCheck) {
        currCheck = condition.Status;
        this._logger.LogDebug("{HealthCheck} path: {path} value: {value} status: {status}", healthCheckIdentifier,
          condition.Path, result, condition.Status);
      }
    } else {
      this._logger.LogError("{HealthCheck} no value match at path {path} expected value {value}", healthCheckIdentifier, condition.Path, condition.Value);
      //if condition not met check use the nonMatch value if it exists.
      if (
           (condition.NoMatchStatus != null)
           && ((condition.NoMatchStatus.Value == HealthStatus.Unknown) || (condition.NoMatchStatus.Value > currCheck))
         ) {
        currCheck = condition.NoMatchStatus.Value;
        this._logger.LogDebug("{HealthCheck} Using the noMatch value {value}", healthCheckIdentifier, condition.NoMatchStatus);
      }
    }

    return currCheck;
  }

}
