using System;
using System.Collections.Generic;

namespace Cms.BatCave.Sonar.Models;

public record UserPermissionsView(
  Dictionary<String, List<String>> PermissionTree);
