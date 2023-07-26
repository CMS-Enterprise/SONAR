import { PermissionConfiguration } from "api/data-contracts";
import { useSonarApi } from "components/SonarApi/Provider";
import { UseQueryResult, useQuery } from "react-query";

export enum QueryKeys {
  GetPermissions = 'getPermissions'
}

export type PermissionConfigurationByEnvironment = {
  [key: string]: PermissionConfiguration[];
}

function groupByEnvironment(grouping: PermissionConfigurationByEnvironment, item: PermissionConfiguration) {
  const environmentName = item.environment || 'Global';
  grouping[environmentName] = grouping[environmentName] || [];
  grouping[environmentName].push(item);
  return grouping;
}

export function usePermissionConfigurationByEnvironment(): UseQueryResult<PermissionConfigurationByEnvironment> {
  const sonarApi = useSonarApi();
  return useQuery({
    queryKey: [QueryKeys.GetPermissions],
    queryFn: () => sonarApi.getPermissions()
      .then((response) => {
        return (response.data || []).reduce(groupByEnvironment, {})
      })
      .catch((error) => {
        console.error(`Error executing ${QueryKeys.GetPermissions} query: `, error);
      })
  });
}
