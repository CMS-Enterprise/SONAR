using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyHealth(
  String Name,
  String DisplayName,
  String? Description,
  Uri? Url,
  DateTime? Timestamp,
  HealthStatus? AggregateStatus,
  IReadOnlyDictionary<String, (DateTime, HealthStatus)?>? HealthChecks,
  IImmutableSet<ServiceHierarchyHealth>? Children);
