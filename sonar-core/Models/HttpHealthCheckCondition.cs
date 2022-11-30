using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Models;

[JsonConverter(typeof(HttpHealthCheckConditionJsonConverter))]
public abstract record HttpHealthCheckCondition(
  HealthStatus Status,
  HttpHealthCheckConditionType Type);
