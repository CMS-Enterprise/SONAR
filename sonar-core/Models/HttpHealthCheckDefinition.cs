using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models;

public sealed record HttpHealthCheckDefinition : HealthCheckDefinition {

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

  public Boolean Equals(HttpHealthCheckDefinition? other) {
    return other is not null &&
      Object.Equals(this.Url, other.Url) &&
      String.Equals(this.FollowRedirects, other.FollowRedirects) &&
      String.Equals(this.AuthorizationHeader, other.AuthorizationHeader) &&
      String.Equals(this.SkipCertificateValidation, other.SkipCertificateValidation) &&
      // JsonSerializer does not respect null constraints
      ((this.Conditions == null && other.Conditions == null) ||
        (this.Conditions != null && other.Conditions != null &&
          this.Conditions.Zip(other.Conditions, Object.Equals).All(x => x)));
  }

  public override Int32 GetHashCode() {
    return HashCode.Combine(
      this.Url,
      this.FollowRedirects,
      this.AuthorizationHeader,
      this.SkipCertificateValidation,
      // JsonSerializer does not respect null constraints
      this.Conditions != null ?
        (Object)HashCodes.From(this.Conditions) :
        null);
  }
}
