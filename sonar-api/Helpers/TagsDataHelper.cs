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
}
