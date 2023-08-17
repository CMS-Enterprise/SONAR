import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import {
  EnvironmentHealth,
  PermissionConfiguration,
  TenantHealth
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

export const initialUserOption: DropdownOptions = {
  label: "Please select User",
  value: 0
}

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
  allEnvironments: EnvironmentHealth[]
) {
  return [initialEnvOption].concat(
    allEnvironments.map((env) => {
      const option: DropdownOptions = {
        label: env.environmentName,
        value: env.environmentName
      }
      return option;
    })
  );
}

export function getTenantOptions(
  allTenants: TenantHealth[],
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
