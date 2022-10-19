using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cms.BatCave.Sonar.Data;

[Table("service_relationship")]
public class ServiceRelationship {
  public Guid ParentServiceId { get; init; }
  public Guid ServiceId { get; init; }

  public ServiceRelationship(
    Guid parentServiceId,
    Guid serviceId) {

    this.ParentServiceId = parentServiceId;
    this.ServiceId = serviceId;
  }
}
