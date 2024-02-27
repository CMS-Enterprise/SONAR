using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(AlertReceiverModelJsonConverter))]
public record AlertReceiverConfiguration {

  public AlertReceiverConfiguration(String name, AlertReceiverType receiverType, AlertReceiverOptions options) {
    this.Name = name;
    this.Type = receiverType;
    this.Options = options;
  }

  [StringLength(100)]
  [Required]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  public String Name { get; init; }

  [Required]
  public AlertReceiverType Type { get; init; }

  [Required]
  public AlertReceiverOptions Options { get; init; }
}
