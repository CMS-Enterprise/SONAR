using System;
using System.ComponentModel.DataAnnotations.Schema;
using Cms.BatCave.Sonar.Enumeration;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("service_version_cache")]
[Index(nameof(Environment), nameof(Tenant), nameof(Service), nameof(VersionCheckType), IsUnique = true)]
public class ServiceVersionCache {
  public Guid Id { get; init; }
  public String Environment { get; init; }
  public String Tenant { get; init; }
  public String Service { get; init; }
  public VersionCheckType VersionCheckType { get; init; }
  public String Version { get; init; }
  public DateTime Timestamp { get; init; }

  public ServiceVersionCache(
    Guid id,
    String environment,
    String tenant,
    String service,
    VersionCheckType versionCheckType,
    String version,
    DateTime timestamp) {
    this.Id = id;
    this.Environment = environment;
    this.Tenant = tenant;
    this.Service = service;
    this.VersionCheckType = versionCheckType;
    this.Version = version;
    this.Timestamp = timestamp;
  }

  public static ServiceVersionCache New(
    String environment,
    String tenant,
    String service,
    VersionCheckType versionCheckType,
    String version,
    DateTime timestamp) =>
    new ServiceVersionCache(Guid.Empty, environment, tenant, service, versionCheckType, version, timestamp);
}
