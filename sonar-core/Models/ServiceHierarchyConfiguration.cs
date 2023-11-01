using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyConfiguration : IValidatableObject {
  public static readonly ServiceHierarchyConfiguration Empty =
    new ServiceHierarchyConfiguration(ImmutableList<ServiceConfiguration>.Empty, ImmutableHashSet<String>.Empty, null);

  public ServiceHierarchyConfiguration(
    IImmutableList<ServiceConfiguration> services,
    IImmutableSet<String> rootServices,
    IImmutableDictionary<String, String?>? tags) {

    this.Services = services;
    this.RootServices = rootServices;
    this.Tags = tags;
  }

  [Required]
  public IImmutableList<ServiceConfiguration> Services { get; init; }

  [Required]
  public IImmutableSet<String> RootServices { get; init; }

  public IImmutableDictionary<String, String?>? Tags { get; init; }

  /// <summary>
  /// Ensures this <see cref="ServiceHierarchyConfiguration"/> meets all of the higher-order validation criteria
  /// that can't expressed via data attributes. This method returns an empty collection if the object is valid,
  /// otherwise it returns a list of the validation errors found.
  /// <list type="number">
  ///   <listheader>
  ///     <description>Validation criteria:</description>
  ///   </listheader>
  ///   <item>
  ///     <description>All service names in the service collection are unique.</description>
  ///   </item>
  ///   <item>
  ///     <description>All declared child services are present in the service collection.</description>
  ///   </item>
  ///   <item>
  ///     <description>All declared root services are present in the service collection.</description>
  ///   </item>
  /// </list>
  /// </summary>
  /// <param name="validationContext">The validation context.</param>
  /// <returns>An empty collection if the object is valid, or a list of errors found if the object is invalid.</returns>
  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
    var validationResults = new List<ValidationResult>();

    var serviceNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    var duplicateServiceNames =
      this.Services.Where(svc => !serviceNames.Add(svc.Name))
        .ToImmutableList();

    if (duplicateServiceNames.Any()) {
      validationResults.Add(new ValidationResult(
        errorMessage: "The specified list of services contained multiple services with the same name.",
        new[] { nameof(this.Services) }));
    }

    var missingRootServices =
      this.RootServices
        .Where(rootSvcName => !serviceNames.Contains(rootSvcName))
        .ToImmutableList();

    if (missingRootServices.Any()) {
      validationResults.Add(new ValidationResult(
        errorMessage: "One or more of the specified root services do not exist in the services array.",
        new[] { nameof(this.RootServices) }));
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
      new[] { nameof(this.Services) }));
    }

    return validationResults;
  }
}
