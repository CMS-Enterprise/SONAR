using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceAlert {
  public ServiceAlert(
    String name,
    HealthStatus threshold,
    String receiverName,
    AlertReceiverType receiverType,
    DateTime? since,
    Boolean isFiring,
    Boolean isSilenced,
    AlertSilenceView? silenceDetails) {

    this.Name = name;
    this.Threshold = threshold;
    this.ReceiverName = receiverName;
    this.ReceiverType = receiverType;
    this.Since = since;
    this.IsFiring = isFiring;
    this.IsSilenced = isSilenced;
    this.SilenceDetails = silenceDetails;

  }

  [Required]
  public String Name { get; init; }

  [Required]
  public HealthStatus Threshold { get; init; }

  [Required]
  public String ReceiverName { get; init; }

  [Required]
  public AlertReceiverType ReceiverType { get; init; }

  public DateTime? Since { get; init; }

  [Required]
  public Boolean IsFiring { get; init; }

  [Required]
  public Boolean IsSilenced { get; init; }

  public AlertSilenceView? SilenceDetails { get; init; }


};
