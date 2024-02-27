using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public record AlertSilenceView {
  public AlertSilenceView(
    DateTime startsAt,
    DateTime endsAt,
    String silencedBy) {

    this.StartsAt = startsAt;
    this.EndsAt = endsAt;
    this.SilencedBy = silencedBy;
  }

  [Required]
  public DateTime StartsAt { get; init; }

  [Required]
  public DateTime EndsAt { get; init; }

  [Required]
  public String SilencedBy { get; init; }
}
