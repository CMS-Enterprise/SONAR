using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Helpers;

public class TagsDataHelper {
  private readonly DataContext _dbContext;
  private readonly DbSet<ServiceTag> _serviceTagsTable;
  private readonly DbSet<TenantTag> _tenantTagsTable;

  public TagsDataHelper(
    DbSet<ServiceTag> serviceTagsTable,
    DbSet<TenantTag> tenantTagsTable,
    DataContext dbContext) {

    this._serviceTagsTable = serviceTagsTable;
    this._tenantTagsTable = tenantTagsTable;
    this._dbContext = dbContext;
  }

  public async Task<IList<TenantTag>> FetchExistingTenantTags(
    Guid tenantId,
    CancellationToken cancellationToken) {

    return
      await this._tenantTagsTable
        .Where(tt => tt.TenantId == tenantId)
        .ToListAsync(cancellationToken);
  }

  public async Task<IList<ServiceTag>> FetchExistingServiceTags(
    IEnumerable<Guid> serviceIds,
    CancellationToken cancellationToken) {

    return await this._serviceTagsTable
      .Where(st => serviceIds.Contains(st.ServiceId))
      .ToListAsync(cancellationToken);
  }

  public void ValidateTenantTags(
    IEnumerable<String> tenantTags,
    IEnumerable<String> serviceTags) {

    if (tenantTags.Any(String.IsNullOrWhiteSpace)) {
      throw new BadRequestException("Invalid tenant tag format");
    }

    if (serviceTags.Any(String.IsNullOrWhiteSpace)) {
      throw new BadRequestException("Invalid service tag format");
    }
  }

  public IImmutableDictionary<String, String?> GetResolvedTenantTags(
    List<TenantTag>? tenantTags) {

    var resolvedTags = new Dictionary<String, String?>();

    if (tenantTags == null || !tenantTags.Any()) {
      return resolvedTags.ToImmutableDictionary();
    }

    foreach (var tag in tenantTags) {
      if (!String.IsNullOrEmpty(tag.Value)) {
        resolvedTags.Add(tag.Name, tag.Value);
      }
    }

    return resolvedTags.ToImmutableDictionary();
  }

  public IImmutableDictionary<String, String?> GetResolvedServiceTags(
    IImmutableDictionary<String, String?> inheritedTags,
    List<ServiceTag>? currServiceTags) {

    if (currServiceTags == null || !currServiceTags.Any()) {
      return inheritedTags;
    }

    var resolvedTags = inheritedTags.ToDictionary(
      kvp => kvp.Key,
      kvp => kvp.Value);

    foreach (var currServiceTag in currServiceTags) {
      // remove tag if value is null
      if (String.IsNullOrEmpty(currServiceTag.Value)) {
        resolvedTags.Remove(currServiceTag.Name);
        continue;
      }

      if (inheritedTags.TryGetValue(currServiceTag.Name, out var existingTagVal)) {
        // tag already inherited, compare values and update if needed
        if (currServiceTag.Value != existingTagVal) {
          resolvedTags[currServiceTag.Name] = currServiceTag.Value;
        }
      } else {
        // tag not inherited, add to resolved tags
        resolvedTags.Add(currServiceTag.Name, currServiceTag.Value);
      }
    }

    return resolvedTags.ToImmutableDictionary();
  }
}
