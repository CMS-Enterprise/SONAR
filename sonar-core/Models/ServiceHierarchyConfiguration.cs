using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cms.BatCave.Sonar.Exceptions;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyConfiguration(
  [Required]
  IImmutableList<ServiceConfiguration> Services,
  [Required]
  IImmutableSet<String> RootServices
) {
  /// <summary>
  /// Ensures this <see cref="ServiceHierarchyConfiguration"/> meets all of the validation criteria that cannot be
  /// expressed via data attributes. This method returns quietly if the object is valid, or throws an exception
  /// indicating the validation error(s) found if it's invalid.
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
  /// <exception cref="InvalidConfigurationException">If any validation errors are found.</exception>
  /// <returns>Nothing (quietly) if validation is successful.</returns>
  public void Validate() {
    var serviceNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    var duplicateServiceNames =
      this.Services.Where(svc => !serviceNames.Add(svc.Name))
        .ToImmutableList();

    if (duplicateServiceNames.Any()) {
      throw new InvalidConfigurationException(
        message: "The specified list of services contained multiple services with the same name.",
        new Dictionary<String, Object?> {
          [nameof(this.Services)] = duplicateServiceNames
        });
    }

    var missingRootServices =
      this.RootServices
        .Where(rootSvcName => !serviceNames.Contains(rootSvcName))
        .ToImmutableList();

    if (missingRootServices.Any()) {
      throw new InvalidConfigurationException(
        message: "One or more of the specified root services do not exist in the services array.",
        new Dictionary<String, Object?> {
          [nameof(this.RootServices)] = missingRootServices
        });
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
      throw new InvalidConfigurationException(
        message:
        "One or more of the specified services contained a reference to a child service that did not exist in the services array.",
        new Dictionary<String, Object?> {
          [nameof(this.Services)] = missingChildServices
        });
    }
  }
}
