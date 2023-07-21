import { EnvironmentUsers } from "./EnvironmentUsersTable";
import { UserPermissions } from "./UserPermissionsTable";

export const envsTableData: EnvironmentUsers[] = [
  { environmentName: 'Gotham', numUsers: 9001 },
  { environmentName: 'WayneManor', numUsers: 2 },
];

export const userPermsTableData: UserPermissions[] = [
  { name: 'Alfred Pennyworth', email: 'alfred.pennyworth@cms.hhs.gov', tenant: 'WayneManor', role: 'Admin' },
  { name: 'Barbara Gordon', email: 'barbara.gordon@cms.hhs.gov', tenant: 'WayneManor', role: 'Admin' },
  { name: 'Bruce Wayne', email: 'bruce.wayne@cms.hhs.gov', tenant: 'WayneManor', role: 'Admin' },
];
