using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Extensions;

public static class DbSetExtensions {
  public static async Task<IImmutableList<T>> AddAllAsync<T>(
    this DbSet<T> source,
    IEnumerable<T> entities,
    CancellationToken cancellationToken = default) where T : class {

    var results = ImmutableList.CreateBuilder<T>();
    foreach (var entity in entities) {
      results.Add((await source.AddAsync(entity, cancellationToken)).Entity);
    }

    return results.ToImmutableList();
  }
}
