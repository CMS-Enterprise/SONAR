using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record TenantHealth(
  String EnvironmentName,
  String TenantName,
  DateTime? Timestamp,
  HealthStatus? AggregateStatus,
  ServiceHierarchyHealth?[] RootServices);

