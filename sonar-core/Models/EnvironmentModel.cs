using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record EnvironmentModel {
  [StringLength(100)]
  [Required]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  public String Name { get; init; }

  public EnvironmentModel(String name) {
    this.Name = name;
  }
}