import { useMutation, useQuery } from 'react-query';
import { EnvironmentModel } from '../../api/data-contracts';
import { useAlertContext } from '../App/AlertContextProvider';
import { useSonarApi } from '../AppContext/AppContextProvider';


export enum QueryKeys {
  GetEnvironments = 'environments',
  GetTenants = 'tenants',
  GetEnvironmentsView = 'environments-view',
  GetTenantsView = 'tenants-view'
}

export const useGetEnvironments = () => {
  const sonarClient = useSonarApi();
  const { createAlert } = useAlertContext();
  return useQuery({
    queryKey: QueryKeys.GetEnvironments,
    queryFn: () => sonarClient.getEnvironments()
      .then((res) => {
        return res.data;
      }),
    onError: () => {
      createAlert(
        "Unable to fetch environments.",
        "System unable to fetch environments from SONAR API. Please try again later.",
        "error",
        QueryKeys.GetEnvironments);
    }
  });
}

export const useGetEnvironmentsView = () => {
  const sonarClient = useSonarApi();
  const { createAlert } = useAlertContext();
  return useQuery({
    queryKey: QueryKeys.GetEnvironmentsView,
    queryFn: () => sonarClient.getEnvironmentsView()
      .then((res) => {
        return res.data;
      }),
    onError: () => {
      createAlert(
        "Unable to fetch environments view.",
        "System unable to fetch environments from SONAR API. Please try again later.",
        "error",
        QueryKeys.GetEnvironmentsView);
    }
  });
}

export const useCreateEnvironment = () => {
  const sonarClient = useSonarApi();
  return useMutation({
    mutationFn: (newEnv: EnvironmentModel) => sonarClient.createEnvironment(newEnv),
  });
}

export const useGetTenants = (enabled: boolean, environment?: string, tenant?: string) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: QueryKeys.GetTenants,
    enabled: enabled,
    queryFn: () => sonarClient.getTenants({
      environment: environment,
      tenant: tenant
    })
      .then((res) => {
        return res.data;
      })
  });
}

export const useGetTenantsView = (enabled: boolean, environment?: string, tenant?: string) => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: QueryKeys.GetTenantsView,
    enabled: enabled,
    queryFn: () => sonarClient.getTenantsView({
      environment: environment,
      tenant: tenant
    })
      .then((res) => {
        return res.data;
      })
  });
}
