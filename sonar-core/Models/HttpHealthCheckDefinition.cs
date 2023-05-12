using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record HttpHealthCheckDefinition : HealthCheckDefinition {

  public HttpHealthCheckDefinition(
    Uri url,
    HttpHealthCheckCondition[] conditions,
    Boolean? followRedirects = false,
    String? authorizationHeader = null,
    Boolean? skipCertificateValidation = false) {

    this.Url = url;
    this.Conditions = conditions;
    this.FollowRedirects = followRedirects;
    this.AuthorizationHeader = authorizationHeader;
    this.SkipCertificateValidation = skipCertificateValidation;
  }

  [Required]
  public Uri Url { get; init; }

  [Required]
  public HttpHealthCheckCondition[] Conditions { get; init; }

  public Boolean? FollowRedirects { get; init; }

  public String? AuthorizationHeader { get; init; }

  public Boolean? SkipCertificateValidation { get; init; }
}
