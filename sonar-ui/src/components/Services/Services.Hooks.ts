import { useQuery } from 'react-query';
import {
  HealthCheckModel,
  ServiceHierarchyConfiguration
} from '../../api/data-contracts';
import { calculateHistoryRange } from '../../helpers/StatusHistoryHelper';
import { useSonarApi } from '../AppContext/AppContextProvider';

export enum QueryKeys {
  StatusHistory = 'StatusHistory'
}
export const useGetServiceHealthCheckData = (
  healthCheck: HealthCheckModel,
  envName: string,
  tenantName: string,
  serviceName: string,
  checkName: string
) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: [`${healthCheck.name}-data`],
    queryFn: () =>
      sonarClient.getHealthCheckData(envName, tenantName, serviceName, checkName)
        .then((res) => {
          return res.data;
        }),
    staleTime: 120000
  });
}

export const useGetServiceHealthHistory = (
  envName: string,
  tenantName: string,
  servicePath: string
) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: [QueryKeys.StatusHistory, envName, tenantName, servicePath],
    queryFn: () =>
      sonarClient.getServiceHealthHistory(envName, tenantName, servicePath, calculateHistoryRange())
        .then((res) => {
          return res.data;
        })
  });
}

export const useGetServiceVersion = (
  envName: string,
  tenantName: string,
  servicePath: string
) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: ['ServiceVersion', envName, tenantName, servicePath],
    queryFn: () => {
      return sonarClient.getSpecificServiceVersionDetails(envName, tenantName, servicePath)
        .then((res) => (res.data || []).sort((v1, v2) => v1.versionType.localeCompare(v2.versionType)))
    }
  });
}

export const useGetHistoricalHealthCheckResults = (
  env: string,
  tenant: string,
  service: string,
  timestamp: string
) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: ['HealthCheckHistory', env, tenant, service, timestamp],
    queryFn: () => {
      return sonarClient.getHistoricalHealthCheckResultsForService(
        env,
        tenant,
        service,
        {
          timeQuery: timestamp
        })
        .then(res => res.data);
    },
  })
}

// Fetches the configuration hierarchy for a tenant, if one is specified.
// Otherwise, returns a Promise that resolves to undefined.
export const useMaybeGetHierarchyConfigQuery = (
  environmentName: string,
  tenantName?: string
) => {
  const sonarClient = useSonarApi();
  return useQuery<ServiceHierarchyConfiguration | undefined, Error>(
    ['ServiceHierarchyConfig', environmentName, tenantName],
    async () => {
      if (tenantName != null) {
        const res = await sonarClient.getTenant(environmentName, tenantName);
        return res.data;
      } else {
        return undefined;
      }
    }
  );
}
