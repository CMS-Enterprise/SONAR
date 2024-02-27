using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models;

public record AlertingConfiguration : IValidatableObject {
  public AlertingConfiguration(IImmutableList<AlertReceiverConfiguration> receivers) {
    this.Receivers = receivers;
  }

  [Required]
  public IImmutableList<AlertReceiverConfiguration> Receivers { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>();

    var receiverNames = this.Receivers.Select(r => r.Name)
      .ToImmutableList();
    var distinctReceiverNames = new HashSet<String>(receiverNames, StringComparer.OrdinalIgnoreCase)
      .ToImmutableHashSet();

    if (distinctReceiverNames.Count != receiverNames.Count) {
      validationResults.Add(new ValidationResult(
        errorMessage: "One or more alert receivers have the same name.",
        memberNames: new[] { nameof(this.Receivers) }));
    }

    return validationResults;
  }
}
