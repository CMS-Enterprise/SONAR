import { useQuery } from 'react-query';
import { HealthCheckModel } from '../../api/data-contracts';
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
