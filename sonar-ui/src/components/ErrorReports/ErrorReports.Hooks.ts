import { useQuery } from 'react-query';
import {
  AgentErrorLevel,
  AgentErrorType
} from '../../api/data-contracts';
import { useSonarApi } from '../AppContext/AppContextProvider';

export enum QueryKeys {
  GetErrorReports = 'errorReports',
  GetErrorReportsForTenant = 'errorReportsForTenant'
}

export const useListErrorReports = (
  environment: string,
  query?: {
    serviceName?: string | undefined,
    healthCheckName?: string | undefined,
    errorLevel?: AgentErrorLevel | undefined,
    errorType?: AgentErrorType | undefined,
    start?: string | undefined,
    end?: string | undefined
  } | undefined
) => {
  const sonarClient = useSonarApi();
  return  useQuery({
    queryKey: [
      QueryKeys.GetErrorReports,
      environment,
      query?.start,
      query?.end
    ],
    queryFn: () => sonarClient.listErrorReports(environment, query)
      .then((res) => {
        return res.data;
      })
  });
}

export const useListErrorReportsForTenant = (
  environment: string,
  tenant: string,
  query?: {
    serviceName?: string | undefined,
    healthCheckName?: string | undefined,
    errorLevel?: AgentErrorLevel | undefined,
    errorType?: AgentErrorType | undefined,
    start?: string | undefined,
    end?: string | undefined
  } | undefined
) => {
  const sonarClient = useSonarApi();
  return  useQuery({
    queryKey: [
      QueryKeys.GetErrorReports,
      environment,
      tenant,
      query?.start,
      query?.end
    ],
    queryFn: () => sonarClient.listErrorReportsForTenant(environment, tenant, query)
      .then((res) => {
        return res.data;
      })
  });
}
