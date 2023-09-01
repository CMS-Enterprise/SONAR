using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record HttpResponseBodyVersionCheckDefinition : VersionCheckDefinition {
  public HttpResponseBodyVersionCheckDefinition(String url, String path, HttpBodyType bodyType) {
    this.Url = url;
    this.Path = path;
    this.BodyType = bodyType;
  }

  [Required]
  public String Url { get; init; }

  [Required]
  public String Path { get; init; }

  [Required]
  public HttpBodyType BodyType { get; init; }
}
