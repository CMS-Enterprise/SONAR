using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("service_tag")]
[Index(nameof(ServiceId), nameof(Name), IsUnique = true)]
public class ServiceTag {
  public Guid Id { get; init; }
  public Guid ServiceId { get; init; }
  public String Name { get; init; }
  public String? Value { get; init; }

  public ServiceTag(
    Guid id,
    Guid serviceId,
    String name,
    String? value) {

    this.Id = id;
    this.ServiceId = serviceId;
    this.Name = name;
    this.Value = value;
  }

  public static ServiceTag New(
    Guid serviceId,
    String name,
    String? value) =>
    new ServiceTag(
      Guid.Empty,
      serviceId,
      name,
      value);
}
