using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record EnvironmentModel {

  public EnvironmentModel(
    String name,
    Boolean isNonProd,
    IImmutableList<ScheduledMaintenanceConfiguration>? scheduledMaintenances = null) {

    this.Name = name;
    this.IsNonProd = isNonProd;
    this.ScheduledMaintenances = scheduledMaintenances;
  }

  [StringLength(100)]
  [Required]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  public String Name { get; init; }

  public Boolean IsNonProd { get; init; }

  public IImmutableList<ScheduledMaintenanceConfiguration>? ScheduledMaintenances { get; init; }
}
