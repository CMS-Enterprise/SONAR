using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Json;
using Json.Path;

namespace Cms.BatCave.Sonar.Models;

public record HttpBodyHealthCheckCondition : HttpHealthCheckCondition {

  public HttpBodyHealthCheckCondition(
    HealthStatus status,
    HttpHealthCheckConditionType type,
    String path,
    String value)
    : base(status, type) {

    this.Path = path;
    this.Value = value;
    this.Type = type;
  }

  [Required]
  public String Path { get; init; }

  [Required]
  public String Value { get; init; }

  public HealthStatus? NoMatchStatus { get; init; }


  public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>(base.Validate(validationContext));

    // Validate that our Value is a valid regular expression.
    try {
      var _ = new Regex(this.Value);
    } catch (Exception e) {
      validationResults.Add(new ValidationResult(
        errorMessage: $"Regular expression is not valid: {e.Message}",
        new[] { nameof(this.Value) }));
    }

    if (this.NoMatchStatus == HealthStatus.Maintenance) {
      validationResults.Add(new ValidationResult(
        errorMessage: $"Invalid {nameof(this.NoMatchStatus)}: The {nameof(HealthStatus)} {nameof(HealthStatus.Maintenance)} is reserved and not a valid health check status."
      ));
    }


    // Validate that our Type is valid, and that our Path is valid according to our type.
    switch (this.Type) {

      case HttpHealthCheckConditionType.HttpBodyJson:
        try {
          var _ = JsonPath.Parse(this.Path);
        } catch (Exception e) {
          validationResults.Add(new ValidationResult(
            errorMessage: $"JsonPath expression is not valid: {e.Message}",
            new[] { nameof(this.Path) }));
        }
        break;

      case HttpHealthCheckConditionType.HttpBodyXml:
        try {
          var _ = XPathExpression.Compile(this.Path);
        } catch (Exception e) {
          validationResults.Add(new ValidationResult(
            errorMessage: $"XPath expression is not valid: {e.Message}",
            new[] { nameof(this.Path) }));
        }
        break;

      // This class should only get deserialized with one of the types relevant to HTTP body conditions.
      // However, we still need to cover all possible Type values in this validator to ensure we don't forget to
      // cover any new HTTP body related ones that get added in the future. Add new types that are not relevant
      // to this class in this fall-through block.
      case HttpHealthCheckConditionType.HttpResponseTime:
      case HttpHealthCheckConditionType.HttpStatusCode:
        throw new InvalidConfigurationException(
          $"Invalid {nameof(this.Type)} value for {typeof(HttpBodyHealthCheckCondition)}: {this.Type}." +
          $"This should not be possible! Verify that {typeof(HttpHealthCheckConditionJsonConverter)} is properly " +
          $"handling all {nameof(HttpHealthCheckCondition.Type)} variations.",
          InvalidConfigurationErrorType.IncompatibleHttpHealthCheckConditionType);

      default:
        throw new InvalidOperationException(
          $"Missing case for {nameof(HttpHealthCheckCondition.Type)}={this.Type} validation in " +
          $"{nameof(HttpBodyHealthCheckCondition.Validate)}.");
    }

    return validationResults;
  }
}
