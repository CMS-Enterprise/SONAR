using System;
using System.Collections.Immutable;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Configuration;

/// <summary>
///   Represents a mapping from a service returned from the legacy (v1) API endpoint to either a custom
///   <paramref cref="DisplayName" /> or a specific <paramref cref="Environment" />
///   <paramref name="Tenant" /> and <paramref name="Name" />.
/// </summary>
/// <param name="LegacyName">
///   The name of the service to return from the legacy endpoint (or by which
///   this service is accessed in the legacy endpoint's path.
/// </param>
/// <param name="DisplayName">
///   The display name for the service. This parameter must be specified if no
///   mapping to an Environment/Tenant/Service triple is supplied. If both are specified the specified
///   value will override the standard <see cref="ServiceConfiguration.DisplayName" />
/// </param>
/// <param name="Environment">The name of the Environment that this entry maps to.</param>
/// <param name="Tenant">The name of the Tenant that this entry maps to.</param>
/// <param name="Name">The name of the Service that this entry maps to.</param>
/// <param name="Children">
///   A list of the names of services that this service depends on. Note: these
///   should correspond to the <paramref name="LegacyName" /> properties of other
///   <see cref="LegacyServiceMapping" /> instances.
/// </param>
public record LegacyServiceMapping(
  String LegacyName,
  String? DisplayName = null,
  String? Environment = null,
  String? Tenant = null,
  String? Name = null,
  String[]? Children = null) {

  public IImmutableSet<String> GetChildren() {
    return this.Children != null ?
      this.Children.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase) :
      ImmutableHashSet<String>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);
  }
}
