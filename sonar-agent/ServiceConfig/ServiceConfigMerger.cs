using System;
using System.Collections.Immutable;
using System.Linq;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public static class ServiceConfigMerger {
  public static ServiceHierarchyConfiguration MergeConfigurations(
    ServiceHierarchyConfiguration prev,
    ServiceHierarchyConfiguration next) {

    var serviceResults = MergeBy(
      prev.Services,
      next.Services,
      match: (svc1, svc2) => String.Equals(svc1.Name, svc2.Name, StringComparison.OrdinalIgnoreCase), MergeServices
    );

    // Merge Root Services
    return new ServiceHierarchyConfiguration(
      serviceResults,
      prev.RootServices.Union(next.RootServices)
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
      nextService.Url ?? prevService.Url, MergeHealthCheckLists(prevService.HealthChecks, nextService.HealthChecks), MergeChildren(prevService.Children, nextService.Children)
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
        match: (hc1, hc2) => String.Equals(hc1.Name, hc2.Name, StringComparison.OrdinalIgnoreCase), MergeHealthChecks
      );
    }
  }

  private static HealthCheckModel MergeHealthChecks(HealthCheckModel prev, HealthCheckModel next) {
    var mergedType = next.Type == default ? prev.Type : next.Type;
    return new HealthCheckModel(
      prev.Name,
      next.Description ?? prev.Description,
      mergedType, MergeDefinitions(prev, mergedType, prev.Definition, next.Definition)
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
          $"incompatible with the health check definition type {prevDefinition.GetType().Name}."
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
            message: $"Unexpected {nameof(HealthCheckType)}: {(Int32)mergedType}"
          );
      }
    }
  }

  private static IImmutableSet<String>? MergeChildren(
    IImmutableSet<String>? prevServiceChildren,
    IImmutableSet<String>? nextServiceChildren) {

    if (prevServiceChildren == null) {
      return nextServiceChildren;
    } else if (nextServiceChildren == null) {
      return prevServiceChildren;
    }

    return prevServiceChildren.Union(nextServiceChildren);
  }

  private static IImmutableList<T> MergeBy<T>(
    IImmutableList<T> first,
    IImmutableList<T> second,
    Func<T, T, Boolean> match,
    Func<T, T, T> merge) where T : class {

    var result = first;
    foreach (var next in second) {
      var prev = first.SingleOrDefault(v => match(v, next));
      result = prev == null ? result.Add(next) : result.Replace(prev, merge(prev, next));
    }

    return result;
  }

}
