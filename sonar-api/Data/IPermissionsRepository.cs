using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;

namespace Cms.BatCave.Sonar.Data;

public interface IPermissionsRepository {
  Task<UserPermission> AddAsync(UserPermission userPermission, CancellationToken cancelToken);
  Task<UserPermission?> GetAsync(Guid userPermissionId, CancellationToken cancelToken);
  Task<UserPermission?> UpdateAsync(UserPermission userPermission, CancellationToken cancelToken);
  Task<Guid> DeleteAsync(Guid userPermissionId, CancellationToken cancelToken);
  Task<List<PermissionConfiguration>> GetPermissionsAsync(Guid userId, CancellationToken cancelToken);
  Task<List<PermissionConfiguration>> GetPermissionsAsync(CancellationToken cancelToken);
  Task<List<PermissionConfiguration>> GetPermissionsScopeAsync(Guid userId, CancellationToken cancelToken);
  Task<List<PermissionConfiguration>> GetPermissionsScopeAsync(Guid? envId, Guid? tenantId, CancellationToken cancelToken);
  Task<(User? user, Environment? Env, Tenant? tenant)> GetObjectsFromNames(String emailName, String? envName, String? tenantName, CancellationToken cancelToken);
  Task<(Environment? environment, Tenant? tenant, User? user)> GetObjectsFromIds(String? emailName, Guid? envId, Guid? tenantId, CancellationToken cancelToken);
  Task<Boolean> GetAdminStatus(Guid userId, CancellationToken cancellationToken);
  Task<UserPermissionsView> GetUserPermissionsView(Guid userId, CancellationToken cancellationToken);
}
