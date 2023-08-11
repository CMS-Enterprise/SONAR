using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace Cms.BatCave.Sonar.Data;

public class DbPermissionRepository : IPermissionsRepository {

  private readonly DataContext _dbContext;
  private readonly DbSet<Environment> _environmentsTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly DbSet<User> _userTable;
  private readonly DbSet<UserPermission> _userPermissionTable;


  public DbPermissionRepository(
    DataContext dbContext,
    DbSet<Environment> environmentsTable,
    DbSet<Tenant> tenantsTable,
    DbSet<UserPermission> userPermissionTable,
    DbSet<User> userTable) {
    this._dbContext = dbContext;
    this._environmentsTable = environmentsTable;
    this._tenantsTable = tenantsTable;
    this._userPermissionTable = userPermissionTable;
    this._userTable = userTable;
  }

  public async Task<UserPermission> AddAsync(UserPermission userPermission, CancellationToken cancelToken) {
    EntityEntry<UserPermission>? permission = null;
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancelToken);
    try {
      // Record new API key
      permission = await this._userPermissionTable.AddAsync(userPermission, cancelToken);
      await this._dbContext.SaveChangesAsync(cancelToken);
      await tx.CommitAsync(cancelToken);
    } catch (Exception exception) {
      await tx.RollbackAsync(cancelToken);

      if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
        throw new ResourceAlreadyExistsException(userPermission.GetType(), new {
          userPermission.UserId,
          userPermission.EnvironmentId,
          userPermission.TenantId,
          userPermission.Permission
        });
      }

      throw;
    }

    return permission.Entity;
  }

  public async Task<UserPermission?> GetAsync(Guid permissionId, CancellationToken cancelToken) {
    return await this._userPermissionTable.FindAsync(new Object?[] { permissionId }, cancelToken);
  }

  public async Task<UserPermission?> UpdateAsync(
    UserPermission userPermission,
    CancellationToken cancelToken) {
    UserPermission? results = null;
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancelToken);
    try {
      // Record updated user permission
      var currentUserPermission = await this._userPermissionTable
        .Where(up => up.Id == userPermission.Id)
        .SingleOrDefaultAsync(cancelToken);

      if (currentUserPermission != null) {
        currentUserPermission.UserId = userPermission.UserId;
        currentUserPermission.EnvironmentId = userPermission.EnvironmentId;
        currentUserPermission.TenantId = userPermission.TenantId;
        currentUserPermission.Permission = userPermission.Permission;
        results = this._userPermissionTable.Update(currentUserPermission).Entity;
      } else {
        throw new ResourceNotFoundException("User does not exist");
      }
      await this._dbContext.SaveChangesAsync(cancelToken);
      await tx.CommitAsync(cancelToken);
    } catch {
      await tx.RollbackAsync(cancelToken);
      throw;
    }
    return results;
  }
  public async Task<Guid> DeleteAsync(Guid userPermissionId, CancellationToken cancelToken) {
    await using var tx =
      await this._dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancelToken);
    try {
      // Get Permission
      var existingPermission = await this._userPermissionTable
        .Where(k => k.Id == userPermissionId)
        .SingleOrDefaultAsync(cancelToken);

      if (existingPermission == null) {
        throw new ResourceNotFoundException(nameof(UserPermission), userPermissionId);
      }

      // Delete
      this._userPermissionTable.Remove(existingPermission);
      // Save
      await this._dbContext.SaveChangesAsync(cancelToken);
      await tx.CommitAsync(cancelToken);
    } catch {
      await tx.RollbackAsync(cancelToken);
      throw;
    }
    return userPermissionId;
  }

  public async Task<List<PermissionConfiguration>> GetPermissionsAsync(Guid userId, CancellationToken cancelToken) {

    var ups = await this._userPermissionTable.Where(up => up.UserId == userId).ToListAsync(cancellationToken: cancelToken);
    var users = await this._userTable.ToListAsync(cancellationToken: cancelToken);
    var envs = await this._environmentsTable.ToDictionaryAsync<Environment, Guid>(x => x.Id, cancellationToken: cancelToken);
    var tenants = await this._tenantsTable.ToDictionaryAsync<Tenant, Guid>(x => x.Id, cancellationToken: cancelToken);

    var result = new List<PermissionConfiguration>();
    foreach (var up in ups) {
      envs.TryGetValue(up.EnvironmentId ?? Guid.Empty, out var env);
      tenants.TryGetValue(up.TenantId ?? Guid.Empty, out var tenant);
      var user = users.FirstOrDefault(u => u.Id == up.UserId);
      if (user != null) {
        result.Add(new PermissionConfiguration(up.Id, user.Email, up.Permission, env?.Name, tenant?.Name));
      }
    }
    return result;
  }

  public async Task<List<PermissionConfiguration>> GetPermissionsScopeAsync(Guid userId, CancellationToken cancelToken) {

    var currentUserPermissions = await this._userPermissionTable.Where(up => up.UserId == userId).ToListAsync(cancellationToken: cancelToken);
    var users = await this._userTable.ToListAsync(cancellationToken: cancelToken);
    var envs = await this._environmentsTable.ToDictionaryAsync<Environment, Guid>(x => x.Id, cancellationToken: cancelToken);
    var tenants = await this._tenantsTable.ToDictionaryAsync<Tenant, Guid>(x => x.Id, cancellationToken: cancelToken);

    var currentUser = users.FirstOrDefault(u => u.Id == userId);

    var upResults = new List<UserPermission>();
    var envsAdminScope = new List<Environment>();
    var tenantsAdminScope = new List<Tenant>();

    foreach (var up in currentUserPermissions) {
      envs.TryGetValue(up.EnvironmentId ?? Guid.Empty, out var env);
      tenants.TryGetValue(up.TenantId ?? Guid.Empty, out var tenant);
      if (env != null) {
        if (tenant != null) {
          //tenant admin scope
          if (up.Permission == PermissionType.Admin) {
            tenantsAdminScope.Add(tenant);
          }
          upResults.Add(up);
        } else {
          //environment admin scope
          if (up.Permission == PermissionType.Admin) {
            envsAdminScope.Add(env);
          }
          upResults.Add(up);
        }
      } else {
        //global scope
        upResults.Add(up);
      }
    }

    //Add all permissions with the current users environments scope
    foreach (var env in envsAdminScope) {
      var ups = await this._userPermissionTable.Where(e => e.EnvironmentId == env.Id).ToListAsync(cancelToken);
      upResults.AddRange(ups);
    }
    //Add all permissions with the current users tenants scope
    foreach (var ten in tenantsAdminScope) {
      var ups = await this._userPermissionTable.Where(up => up.TenantId == ten.Id).ToListAsync(cancelToken);
      upResults.AddRange(ups);
    }

    var pgResults = new Dictionary<Guid, PermissionConfiguration>();
    foreach (var up in upResults) {
      var user = users.FirstOrDefault(u => u.Id == up.UserId);
      if (user != null) {
        envs.TryGetValue(up.EnvironmentId ?? Guid.Empty, out var env);
        tenants.TryGetValue(up.TenantId ?? Guid.Empty, out var tenant);
        pgResults.TryAdd(
          up.Id,
          new PermissionConfiguration(up.Id, user.Email, up.Permission, env?.Name, tenant?.Name)
        );
      }
    }
    return pgResults.Values.ToList();
  }


  public async Task<List<PermissionConfiguration>> GetPermissionsScopeAsync(Guid? envId, Guid? tenantId, CancellationToken cancelToken) {

    var currentUserPermissions = await this._userPermissionTable.Where(up => ((up.EnvironmentId == envId) && (up.TenantId == tenantId))).ToListAsync(cancellationToken: cancelToken);
    var users = await this._userTable.ToListAsync(cancellationToken: cancelToken);
    var envs = await this._environmentsTable.ToDictionaryAsync<Environment, Guid>(x => x.Id, cancellationToken: cancelToken);
    var tenants = await this._tenantsTable.ToDictionaryAsync<Tenant, Guid>(x => x.Id, cancellationToken: cancelToken);

    var upResults = new List<UserPermission>();
    var envsAdminScope = new List<Environment>();
    var tenantsAdminScope = new List<Tenant>();

    foreach (var up in currentUserPermissions) {
      envs.TryGetValue(up.EnvironmentId ?? Guid.Empty, out var env);
      tenants.TryGetValue(up.TenantId ?? Guid.Empty, out var tenant);
      if (env != null) {
        if (tenant != null) {
          //tenant admin scope
          if (up.Permission == PermissionType.Admin) {
            tenantsAdminScope.Add(tenant);
          }
          upResults.Add(up);
        } else {
          //environment admin scope
          if (up.Permission == PermissionType.Admin) {
            envsAdminScope.Add(env);
          }
          upResults.Add(up);
        }
      } else {
        //global scope
        upResults.Add(up);
      }
    }

    //Add all permissions with the current users environments scope
    foreach (var env in envsAdminScope) {
      var ups = await this._userPermissionTable.Where(e => e.EnvironmentId == env.Id).ToListAsync(cancelToken);
      upResults.AddRange(ups);
    }
    //Add all permissions with the current users tenants scope
    foreach (var ten in tenantsAdminScope) {
      var ups = await this._userPermissionTable.Where(up => up.TenantId == ten.Id).ToListAsync(cancelToken);
      upResults.AddRange(ups);
    }

    var pgResults = new Dictionary<Guid, PermissionConfiguration>();
    foreach (var up in upResults) {
      var user = users.FirstOrDefault(u => u.Id == up.UserId);
      if (user != null) {
        envs.TryGetValue(up.EnvironmentId ?? Guid.Empty, out var env);
        tenants.TryGetValue(up.TenantId ?? Guid.Empty, out var tenant);
        pgResults.TryAdd(
          up.Id,
          new PermissionConfiguration(up.Id, user.Email, up.Permission, env?.Name, tenant?.Name)
        );
      }
    }
    return pgResults.Values.ToList();
  }

  public async Task<List<PermissionConfiguration>> GetPermissionsAsync(CancellationToken cancelToken) {
    var ups = await this._userPermissionTable.ToListAsync(cancellationToken: cancelToken);
    var users = await this._userTable.ToListAsync(cancellationToken: cancelToken);
    var envs = await this._environmentsTable.ToDictionaryAsync<Environment, Guid>(x => x.Id, cancellationToken: cancelToken);
    var tenants = await this._tenantsTable.ToDictionaryAsync<Tenant, Guid>(x => x.Id, cancellationToken: cancelToken);

    var result = new List<PermissionConfiguration>();
    foreach (var up in ups) {
      envs.TryGetValue(up.EnvironmentId ?? Guid.Empty, out var env);
      tenants.TryGetValue(up.TenantId ?? Guid.Empty, out var tenant);
      var user = users.FirstOrDefault(u => (u.Id == up.UserId));
      if (user != null) {
        result.Add(new PermissionConfiguration(up.Id, user.Email, up.Permission, env?.Name, tenant?.Name));
      }
    }
    return result;
  }

  public async Task<(User? user, Environment? Env, Tenant? tenant)> GetObjectsFromNames(
    String? emailName,
    String? envName,
    String? tenantName,
    CancellationToken cancelToken) {
    var environment =
      !String.IsNullOrEmpty(envName) ?
        await this._environmentsTable.FirstOrDefaultAsync(e => e.Name == envName,
          cancelToken) :
        null;
    var tenant =
      !String.IsNullOrEmpty(tenantName) ?
        await this._tenantsTable.FirstOrDefaultAsync(t => t.Name == tenantName, cancelToken) :
        null;

    var user =
      !String.IsNullOrEmpty(emailName) ?
        await this._userTable.FirstOrDefaultAsync(u => u.Email == emailName, cancelToken) :
        null;

    return (user, environment, tenant);
  }

  public async Task<(Environment? environment, Tenant? tenant, User? user)> GetObjectsFromIds(
    String? emailName,
    Guid? envId,
    Guid? tenantId,
    CancellationToken cancelToken) {
    var environment =
      envId != null ?
        await this._environmentsTable.FirstOrDefaultAsync(e => e.Id == envId,
          cancelToken) :
        null;
    var tenant =
      tenantId != null ?
        await this._tenantsTable.FirstOrDefaultAsync(t => t.Id == tenantId, cancelToken) :
        null;

    var user =
      !String.IsNullOrEmpty(emailName) ?
        await this._userTable.FirstOrDefaultAsync(u => u.Email == emailName, cancelToken) :
        null;

    return (environment, tenant, user);
  }

}
