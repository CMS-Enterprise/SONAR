using System;
using System.Collections.Immutable;
using System.Linq;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public static class ServiceConfigMerger {
  /// <summary>
  ///   Deeply merges two <see cref="ServiceHierarchyConfiguration" /> instances, where values specified
  ///   in the <paramref name="next" /> instance override values specified in the
  ///   <paramref name="prev" /> instance.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The contents of the <see cref="ServiceHierarchyConfiguration.Services" /> collections are
  ///     merged based on <see cref="ServiceConfiguration.Name" /> (case insensitive). Any
  ///     <see cref="ServiceConfiguration" /> that exists in one of the collections but not the other are
  ///     included in the resulting <see cref="ServiceHierarchyConfiguration" /> unchanged (Note: this
  ///     means it is not possible to <em>remove</em> services from the collection, only to add or
  ///     modified them).
  ///   </para>
  ///   <para>
  ///     The contents of the <see cref="ServiceHierarchyConfiguration.RootServices" /> collections are
  ///     merged by set union (case insensitive).
  ///   </para>
  ///   <para>
  ///     In addition to the basic properties of <see cref="ServiceConfiguration" />, the
  ///     <see cref="ServiceConfiguration.HealthChecks" /> collection is also deeply merged. The
  ///     <see cref="HealthCheckModel" /> instances are merged by <see cref="HealthCheckModel.Name" />
  ///     (case insensitive). It is possible to change the <see cref="HealthCheckType" /> of a health
  ///     check from one layer of configuration to another, however, when this is done it is necessary to
  ///     provided an new <see cref="HealthCheckModel.Definition" /> that matches the new type. In the
  ///     event, properties from the previous, incompatible <see cref="HealthCheckDefinition" /> will be
  ///     ignored.
  ///   </para>
  ///   <para>
  ///     Both the <see cref="MetricHealthCheckDefinition" /> type and hte
  ///     <see cref="HttpHealthCheckDefinition" /> contain lists of conditions used to determine the
  ///     result of the health check. The conditions do not have a unique identifier and therefor cannot
  ///     be merged in the same way that other collections in this data structure are merged. Instead, if
  ///     a non-null set of conditions is provided in the <paramref name="next" /> data structure, it
  ///     will completely replace the corresponding conditions from the <paramref name="prev" />
  ///     configuration.
  ///   </para>
  /// </remarks>
  public static ServiceHierarchyConfiguration MergeConfigurations(
    ServiceHierarchyConfiguration prev,
    ServiceHierarchyConfiguration next) {

    var serviceResults = MergeBy(
      prev.Services,
      next.Services,
      match: (svc1, svc2) =>
        String.Equals(svc1.Name, svc2.Name, StringComparison.OrdinalIgnoreCase),
      MergeServices
    );

    // Merge Root Services
    return new ServiceHierarchyConfiguration(
      serviceResults,
      NullableSetUnion(prev.RootServices, next.RootServices) ?? ImmutableHashSet<String>.Empty,
      null
    );
  }

  private static ServiceConfiguration MergeServices(
    ServiceConfiguration prevService,
    ServiceConfiguration nextService) {

    return new ServiceConfiguration(
      // Both services should have identical names
      prevService.Name,
      // JsonSerializer does not respect nullability constraints, so DisplayName can actually be
      // null here
      nextService.DisplayName ?? prevService.DisplayName,
      nextService.Description ?? prevService.Description,
      nextService.Url ?? prevService.Url,
      MergeHealthCheckLists(prevService.HealthChecks, nextService.HealthChecks),
      MergeVersionCheckLists(prevService.VersionChecks, nextService.VersionChecks),
      NullableSetUnion(prevService.Children, nextService.Children)
    );
  }

  private static IImmutableList<HealthCheckModel>? MergeHealthCheckLists(
    IImmutableList<HealthCheckModel>? prevHealthChecks,
    IImmutableList<HealthCheckModel>? nextHealthChecks) {

    if (prevHealthChecks == null) {
      return nextHealthChecks;
    } else if (nextHealthChecks == null) {
      return prevHealthChecks;
    } else {
      return MergeBy(
        prevHealthChecks,
        nextHealthChecks,
        match: (hc1, hc2) =>
          String.Equals(hc1.Name, hc2.Name, StringComparison.OrdinalIgnoreCase),
        MergeHealthChecks
      );
    }
  }

  private static HealthCheckModel MergeHealthChecks(HealthCheckModel prev, HealthCheckModel next) {
    var mergedType = next.Type == default ? prev.Type : next.Type;
    return new HealthCheckModel(
      prev.Name,
      next.Description ?? prev.Description,
      mergedType,
      MergeDefinitions(prev, mergedType, prev.Definition, next.Definition),
      next.SmoothingTolerance ?? prev.SmoothingTolerance
    );
  }

  private static HealthCheckDefinition MergeDefinitions(
    HealthCheckModel parent,
    HealthCheckType mergedType,
    HealthCheckDefinition prevDefinition,
    HealthCheckDefinition? nextDefinition) {

    if (nextDefinition == null) {
      // Ensure that the HealthCheckType wasn't changed to something that is
      // incompatible with the HealthCheckDefinition
      var incompatible = mergedType switch {
        HealthCheckType.PrometheusMetric when prevDefinition is not MetricHealthCheckDefinition => true,
        HealthCheckType.LokiMetric when prevDefinition is not MetricHealthCheckDefinition => true,
        HealthCheckType.HttpRequest when prevDefinition is not HttpHealthCheckDefinition => true,
        _ => false
      };

      if (incompatible) {
        throw new InvalidConfigurationException(
          $"The health check {parent.Name} was changed to type {mergedType} which is " +
          $"incompatible with the health check definition type {prevDefinition.GetType().Name}.",
          InvalidConfigurationErrorType.IncompatibleHealthCheckType
        );
      }

      return prevDefinition;
    } else {
      switch (mergedType) {
        case HealthCheckType.PrometheusMetric:
        case HealthCheckType.LokiMetric:
          if (nextDefinition is MetricHealthCheckDefinition nextMetricDefinition) {
            if (prevDefinition is MetricHealthCheckDefinition prevMetricDefinition) {
              return new MetricHealthCheckDefinition(
                nextMetricDefinition.Duration == default ?
                  prevMetricDefinition.Duration :
                  nextMetricDefinition.Duration,
                nextMetricDefinition.Expression ?? prevMetricDefinition.Expression,
                // Health check conditions fully replace the previous set if any are specified
                nextMetricDefinition.Conditions ?? prevMetricDefinition.Conditions
              );
            } else {
              // The health check type was changed, ignore the original definition and use the
              // replacement
              return nextDefinition;
            }
          } else {
            // This should not happen because the deserializer cannot deserialize a
            // HealthCheckDefinition that isn't compatible with the specified HealthCheckType.
            throw new InvalidOperationException(
              "The specified health check definition is not compatible with the expected health check type."
            );
          }

        case HealthCheckType.HttpRequest:
          if (nextDefinition is HttpHealthCheckDefinition nextHttpDefinition) {
            if (prevDefinition is HttpHealthCheckDefinition prevHttpDefinition) {
              return new HttpHealthCheckDefinition(
                nextHttpDefinition.Url ?? prevHttpDefinition.Url,
                nextHttpDefinition.Conditions ?? prevHttpDefinition.Conditions,
                nextHttpDefinition.FollowRedirects ?? prevHttpDefinition.FollowRedirects,
                nextHttpDefinition.AuthorizationHeader ?? prevHttpDefinition.AuthorizationHeader,
                nextHttpDefinition.SkipCertificateValidation ?? prevHttpDefinition.SkipCertificateValidation
              );
            } else {
              // The health check type was changed, ignore the original definition and use the
              // replacement
              return nextDefinition;
            }
          } else {
            // This should not happen because the deserializer cannot deserialize a
            // HealthCheckDefinition that isn't compatible with the specified HealthCheckType.
            throw new InvalidOperationException(
              "The specified health check definition is not compatible with the expected health check type."
            );
          }

        default:
          throw new ArgumentOutOfRangeException(
            nameof(mergedType),
            mergedType,
            $"Unexpected {nameof(HealthCheckType)}: {(Int32)mergedType}"
          );
      }
    }
  }

  private static IImmutableList<VersionCheckModel>? MergeVersionCheckLists(
    IImmutableList<VersionCheckModel>? prevVersionChecks,
    IImmutableList<VersionCheckModel>? nextVersionChecks) {

    if (prevVersionChecks == null) {
      return nextVersionChecks;
    } else if (nextVersionChecks == null) {
      return prevVersionChecks;
    } else {
      return MergeBy(
        prevVersionChecks,
        nextVersionChecks,
        match: (hc1, hc2) =>
          String.Equals(hc1.VersionCheckType.ToString(), hc2.VersionCheckType.ToString(), StringComparison.OrdinalIgnoreCase),
        MergeVersionChecks
      );
    }
  }

  private static VersionCheckModel MergeVersionChecks(VersionCheckModel prev, VersionCheckModel next) {
    var mergedType = next.VersionCheckType == default ? prev.VersionCheckType : next.VersionCheckType;
    return new VersionCheckModel(
      prev.VersionCheckType,
      MergeVersionCheckDefinitions(prev, mergedType, prev.Definition, next.Definition));
  }

  private static VersionCheckDefinition MergeVersionCheckDefinitions(
    VersionCheckModel parent,
    VersionCheckType mergedType,
    VersionCheckDefinition prevDefinition,
    VersionCheckDefinition? nextDefinition) {

    if (nextDefinition == null) {
      // TODO: add incompatible merge logic here when there is another VersionCheckType
      return prevDefinition;
    } else {
      switch (mergedType) {
        case VersionCheckType.FluxKustomization:
          if (nextDefinition is FluxKustomizationVersionCheckDefinition nextFluxVersionCheckDefinition) {
            if (prevDefinition is FluxKustomizationVersionCheckDefinition prevFluxVersionCheckDefinition) {
              return new FluxKustomizationVersionCheckDefinition(
                nextFluxVersionCheckDefinition.K8sNamespace ?? prevFluxVersionCheckDefinition.K8sNamespace,
                nextFluxVersionCheckDefinition.Kustomization ?? prevFluxVersionCheckDefinition.Kustomization);
            } else {
              return nextDefinition;
            }
          } else {
            // This should not happen because the deserializer cannot deserialize a
            // VersionCheckDefinition that isn't compatible with the specified VersionCheckType.
            throw new InvalidOperationException(
              "The specified version check definition is not compatible with the expected version check type."
            );
          }

        case VersionCheckType.HttpResponseBody:
          if (nextDefinition is HttpResponseBodyVersionCheckDefinition nextHttpVersionCheckDefinition) {
            if (prevDefinition is HttpResponseBodyVersionCheckDefinition prevHttpVersionCheckDefinition) {
              var mergedBodyType = nextHttpVersionCheckDefinition.BodyType == default
                ? prevHttpVersionCheckDefinition.BodyType
                : nextHttpVersionCheckDefinition.BodyType;

              return new HttpResponseBodyVersionCheckDefinition(
                nextHttpVersionCheckDefinition.Url ?? prevHttpVersionCheckDefinition.Url,
                nextHttpVersionCheckDefinition.Path ?? prevHttpVersionCheckDefinition.Path,
                mergedBodyType);
            } else {
              return nextDefinition;
            }
          } else {
            // This should not happen because the deserializer cannot deserialize a
            // VersionCheckDefinition that isn't compatible with the specified VersionCheckType.
            throw new InvalidOperationException(
              "The specified version check definition is not compatible with the expected version check type."
            );
          }

        default:
          throw new ArgumentOutOfRangeException(
            nameof(mergedType),
            mergedType,
            $"Unexpected {nameof(VersionCheckType)}: {(Int32)mergedType}"
          );
      }
    }
  }

  private static IImmutableSet<String>? NullableSetUnion(
    IImmutableSet<String>? prevSet,
    IImmutableSet<String>? nextSet) {

    if (prevSet == null) {
      return nextSet;
    } else if (nextSet == null) {
      return prevSet;
    }

    return prevSet.Union(nextSet, StringComparer.OrdinalIgnoreCase).ToImmutableHashSet();
  }

  private static IImmutableList<T> MergeBy<T>(
    IImmutableList<T> first,
    IImmutableList<T>? second,
    Func<T, T, Boolean> match,
    Func<T, T, T> merge) where T : class {


    if (second == null) {
      return first;
    }

    var result = first;
    foreach (var next in second) {
      var prev = first.SingleOrDefault(v => match(v, next));
      result = prev == null ? result.Add(next) : result.Replace(prev, merge(prev, next));
    }

    return result;
  }
}
