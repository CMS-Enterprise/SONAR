using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("alert_receiver")]
[Index(nameof(TenantId), nameof(Name), IsUnique = true)]
public class AlertReceiver {
  public Guid Id { get; init; }
  public Guid TenantId { get; init; }

  [StringLength(100)]
  public String Name { get; init; }
  public AlertReceiverType Type { get; init; }
  public String Options { get; init; }

  public AlertReceiver(
    Guid id,
    Guid tenantId,
    String name,
    AlertReceiverType type,
    String options) {

    this.Id = id;
    this.TenantId = tenantId;
    this.Name = name;
    this.Type = type;
    this.Options = options;
  }

  public static AlertReceiver New(
    Guid tenantId,
    AlertReceiverConfiguration alertReceiverConfiguration) =>
    new AlertReceiver(
      id: default,
      tenantId,
      alertReceiverConfiguration.Name,
      alertReceiverConfiguration.Type,
      SerializeOptions(alertReceiverConfiguration.Options));

  public static String SerializeOptions(AlertReceiverOptions options) {
    return JsonSerializer.Serialize(options, options.GetType(), OptionsSerializerOptions);
  }

  public AlertReceiverOptions DeserializeOptions() {
    return this.Type switch {
      AlertReceiverType.Email =>
        JsonSerializer.Deserialize<AlertReceiverOptionsEmail>(this.Options, OptionsSerializerOptions) ??
        throw new InvalidOperationException("Options deserialized to null."),

      _ => throw new NotSupportedException(
        $"Unable to deserialize options. Unsupported alert receiver type: {this.Type}")
    };
  }

  private static readonly JsonSerializerOptions OptionsSerializerOptions = new() {
    Converters = { new JsonStringEnumConverter() },
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };
}
