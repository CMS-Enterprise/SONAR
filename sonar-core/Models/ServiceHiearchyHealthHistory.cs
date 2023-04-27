using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record ServiceHierarchyHealthHistory(
    String Name,
    String DisplayName,
    String? Description,
    Uri? Url,
    IImmutableList<(DateTime Timestamp, HealthStatus AggregateStatus)>? AggregateStatus,
    IImmutableSet<ServiceHierarchyHealthHistory>? Children);
