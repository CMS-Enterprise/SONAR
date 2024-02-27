using System;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models;

public sealed record AlertReceiverOptionsEmail : AlertReceiverOptions {

  public AlertReceiverOptionsEmail(String address) {
    this.Address = address;
  }

  [Required]
  [EmailAddress]
  public String Address { get; init; }

}
