using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record AlertSilenceDetails {
  public AlertSilenceDetails(
    String name) {

    this.Name = name;
  }

  [Required]
  public String Name { get; init; }
};
