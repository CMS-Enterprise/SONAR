using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Helpers;

public class UserDataHelper {
  private readonly DbSet<User> _userTable;

  public UserDataHelper(DbSet<User> userTable) {
    this._userTable = userTable;
  }

  public async Task<User?> FetchUserByEmailAsync(String userEmail, CancellationToken cancellationToken) {
    // Attempt to fetch existing user from db
    return await this._userTable
      .Where(e => e.Email == userEmail)
      .SingleOrDefaultAsync(cancellationToken);
  }

  public async Task<IList<User>> FetchByUserIdsAsync(List<Guid> ids, CancellationToken cancellationToken) {

    var result =
      await this._userTable.Where(e => ids.Contains(e.Id))
        .ToListAsync(cancellationToken);

    return result;
  }
}
