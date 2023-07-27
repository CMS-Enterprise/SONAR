import { useMutation, useQuery } from 'react-query';
import { EnvironmentModel } from '../../api/data-contracts';
import { useSonarApi } from '../AppContext/AppContextProvider';


export enum QueryKeys {
  GetEnvironments = 'environments',
  GetTenants = 'tenants'
}
export const useGetEnvironments = () => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: QueryKeys.GetEnvironments,
    queryFn: () => sonarClient.getEnvironments()
      .then((res) => {
        return res.data;
      })
  });
}

export const useCreateEnvironment = () => {
  const sonarClient = useSonarApi();
  return useMutation({
    mutationFn: (newEnv: EnvironmentModel) => sonarClient.createEnvironment(newEnv),
  });
}

export const useGetTenants = (enabled: boolean) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: QueryKeys.GetTenants,
    enabled: enabled,
    queryFn: () => sonarClient.getTenants()
      .then((res) => {
        return res.data;
      })
  });
}
