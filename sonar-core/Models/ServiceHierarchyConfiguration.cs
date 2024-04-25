using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyConfiguration : IValidatableObject {
  public static readonly ServiceHierarchyConfiguration Empty =
    new ServiceHierarchyConfiguration(
      ImmutableList<ServiceConfiguration>.Empty,
      ImmutableHashSet<String>.Empty);

  public ServiceHierarchyConfiguration(
    IImmutableList<ServiceConfiguration> services,
    IImmutableSet<String> rootServices,
    IImmutableDictionary<String, String?>? tags = null,
    AlertingConfiguration? alerting = null,
    IImmutableList<ScheduledMaintenanceConfiguration>? scheduledMaintenances = null) {

    this.Services = services;
    this.RootServices = rootServices;
    this.Tags = tags;
    this.Alerting = alerting;
    this.ScheduledMaintenances = scheduledMaintenances;
  }

  [Required]
  public IImmutableList<ServiceConfiguration> Services { get; init; }

  [Required]
  public IImmutableSet<String> RootServices { get; init; }

  public IImmutableDictionary<String, String?>? Tags { get; init; }

  public AlertingConfiguration? Alerting { get; init; }

  public IImmutableList<ScheduledMaintenanceConfiguration>? ScheduledMaintenances { get; init; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>();

    var serviceNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    var duplicateServiceNames =
      this.Services.Where(svc => !serviceNames.Add(svc.Name))
        .ToImmutableList();

    if (duplicateServiceNames.Any()) {
      validationResults.Add(new ValidationResult(
        errorMessage: "The specified list of services contained multiple services with the same name.",
        memberNames: new[] { nameof(this.Services) }));
    }

    var missingRootServices =
      this.RootServices
        .Where(rootSvcName => !serviceNames.Contains(rootSvcName))
        .ToImmutableList();

    if (missingRootServices.Any()) {
      validationResults.Add(new ValidationResult(
        errorMessage: "One or more of the specified root services do not exist in the services array.",
        memberNames: new[] { nameof(this.RootServices) }));
    }

    var missingChildServices =
      this.Services
        .Select(svc =>
          new {
            svc.Name,
            Children = svc.Children?.Where(child => !serviceNames.Contains(child)).ToImmutableList()
          })
        .Where(v => v.Children?.Any() == true)
        .ToImmutableList();

    if (missingChildServices.Any()) {
      validationResults.Add(new ValidationResult(
        errorMessage: "One or more of the specified services contained a reference to a child service that did not exist in the services array.",
        memberNames: new[] { nameof(this.Services) }));
    }

    var alertReceiverNames = (this.Alerting?.Receivers.Select(r => r.Name) ?? ImmutableList<String>.Empty)
      .ToImmutableList();
    var referencedAlertReceiverNames = this.Services.Where(s => s.AlertingRules?.Any() == true)
      .SelectMany(s => s.AlertingRules ?? ImmutableList<AlertingRuleConfiguration>.Empty)
      .Select(r => r.ReceiverName)
      .ToImmutableList();
    var missingReferencedAlertReceiverNames = referencedAlertReceiverNames.Where(n =>
      !alertReceiverNames.Contains(n, StringComparer.OrdinalIgnoreCase));

    if (missingReferencedAlertReceiverNames.Any()) {
      validationResults.Add(new ValidationResult(
        errorMessage: "One or more service alerting rules references an undefined alert receiver name.",
        memberNames: new[] { nameof(AlertingRuleConfiguration.ReceiverName) }));
    }

    return validationResults;
  }
}
