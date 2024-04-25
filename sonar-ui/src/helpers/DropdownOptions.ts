import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import {
  EnvironmentModel,
  PermissionConfiguration,
  ServiceHierarchyInfo,
  TenantInfo
} from '../api/data-contracts';
import { UsersByEmail } from '../pages/UserPermissions.Hooks';

const permissions: DropdownOptions[] = [
  {
    label: "Admin",
    value: "Admin"
  },
  {
    label: "Standard",
    value: "Standard"
  }
];

export const initialRoleOption: DropdownOptions = {
  label: "Please Select Role",
  value: 0
}

export const roles = [initialRoleOption, ...permissions];

export const initialEnvOption: DropdownOptions = {
  label: "All Environments",
  value: 0
}

export const initialTenantOption: DropdownOptions = {
  label: "All Tenants",
  value: 0
}

export const initialServiceOption: DropdownOptions = {
  label: "All Services",
  value: 0
}

export const requiredEnvOption: DropdownOptions = {
  label: "Please Select Environment",
  value: 0
}

export const initialUserOption: DropdownOptions = {
  label: "Please select User",
  value: 0
}

export enum StatusStartTimes {
  Last6Hours,
  Last10Hours,
  Last12Hours,
  Last24Hours,
  Last48Hours,
  LastWeek ,
  CustomTimeRange
}

export const statusTimeRangeOptions: DropdownOptions[] = [
  {
    label: "Last 6 hours",
    value: StatusStartTimes.Last6Hours
  },
  {
    label: "Last 10 hours",
    value: StatusStartTimes.Last10Hours
  },
  {
    label: "Last 12 hours",
    value: StatusStartTimes.Last12Hours
  },
  {
    label: "Last 24 hours",
    value: StatusStartTimes.Last24Hours
  },
  {
    label: "Last 48 hours",
    value: StatusStartTimes.Last48Hours
  },
  {
    label: "Last week",
    value: StatusStartTimes.LastWeek
  },
  {
    label: "Custom range (max=7days)",
    value: StatusStartTimes.CustomTimeRange
  }
];

export function getPermissionOptions(
  allPermissions: PermissionConfiguration[]
) {
  return [initialRoleOption].concat(
    allPermissions.map((role) => {
      const option: DropdownOptions = {
        label: role.permission,
        value: role.permission!
      }
      return option;
    })
  );
}

export function getEnvironmentOptions(
  allEnvironments: EnvironmentModel[]
) {
  return [requiredEnvOption].concat(
    allEnvironments.map((env) => {
      const option: DropdownOptions = {
        label: env.name,
        value: env.name
      }
      return option;
    })
  );
}

export function getTenantOptions(
  allTenants: TenantInfo[],
  selectedEnvironment: DropdownValue
) {
  const tenantOptions = allTenants
    .filter(tenant => tenant.environmentName === selectedEnvironment)
    .map((tenant) => {
      const option: DropdownOptions = {
        label: tenant.tenantName,
        value: tenant.tenantName
      }
      return option;
    })
  return tenantOptions;
}

export function getServiceOptionsByTenant(allTenants: TenantInfo[], selectedEnvironment: DropdownValue, selectedTenant: DropdownValue) {
  return allTenants.filter(t => t.environmentName === selectedEnvironment && t.tenantName === selectedTenant).flatMap((tenant) => {
    const flattenedChildren = flattenChildren(tenant.rootServices);
    return flattenedChildren.map(c => {
      const option: DropdownOptions = {
        label: c,
        value: c
      }
      return option;
    })
  })
}

function flattenChildren(services: ServiceHierarchyInfo[] | null | undefined): string[] {
  return services ?
    services.flatMap(({name, children}) => [
      name,
      ...flattenChildren(children)
    ]) : [];
}

export function getUserOptions(
  allUsersByEmail: UsersByEmail,
  currentUserEmail: string
) {
  return [initialUserOption].concat(
    Object.keys(allUsersByEmail)
      .filter(user => user !== currentUserEmail)
    .map((key, i) => {
      const option: DropdownOptions = {
        label: `${allUsersByEmail[key]} (${key})`,
        value: key
      }
      return option;
    })
  );
}
