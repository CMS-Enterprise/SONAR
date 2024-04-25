import { useMutation, useQueries, useQueryClient } from 'react-query';
import {
  AdHocMaintenanceConfiguration,
  MaintenanceScope
} from '../../api/data-contracts';
import { useSonarApi } from '../AppContext/AppContextProvider';

export enum MaintenanceQueryKeys {
  GetAdhocMaintenances = 'adhocMaintenances',
  GetScheduledMaintenances = 'scheduledMaintenances',
  EnvironmentScoped = 'envScoped',
  TenantScoped = 'tenantScoped',
  ServiceScoped = 'serviceScoped'
}

export const useGetActiveAdHocMaintenances = () => {
  const sonarClient = useSonarApi();
  return useQueries([
    {
      queryKey: [MaintenanceQueryKeys.GetAdhocMaintenances, MaintenanceQueryKeys.EnvironmentScoped],
      queryFn: () => sonarClient.getActiveAdHocEnvironmentMaintenance()
        .then((res) => {
          return res.data;
        })
    },
    {
      queryKey: [MaintenanceQueryKeys.GetAdhocMaintenances, MaintenanceQueryKeys.TenantScoped],
      queryFn: () => sonarClient.getActiveAdHocTenantMaintenance()
        .then((res) => {
          return res.data;
        })
    },
    {
      queryKey: [MaintenanceQueryKeys.GetAdhocMaintenances, MaintenanceQueryKeys.ServiceScoped],
      queryFn: () => sonarClient.getActiveAdHocServiceMaintenance()
        .then((res) => {
          return res.data;
        })
    }
  ]);
}

export const useGetActiveScheduledMaintenances = () => {
  const sonarClient = useSonarApi();
  return useQueries([
    {
      queryKey: [MaintenanceQueryKeys.GetScheduledMaintenances, MaintenanceQueryKeys.EnvironmentScoped],
      queryFn: () => sonarClient.getActiveScheduledEnvironmentMaintenance()
        .then((res) => {
          return res.data;
        })
    },
    {
      queryKey: [MaintenanceQueryKeys.GetScheduledMaintenances, MaintenanceQueryKeys.TenantScoped],
      queryFn: () => sonarClient.getActiveScheduledTenantMaintenance()
        .then((res) => {
          return res.data;
        })
    },
    {
      queryKey: [MaintenanceQueryKeys.GetScheduledMaintenances, MaintenanceQueryKeys.ServiceScoped],
      queryFn: () => sonarClient.getActiveScheduledServiceMaintenance()
        .then((res) => {
          return res.data;
        })
    }
  ]);
}

export const useToggleAdhocMaintenance = (
  env: string,
  tenant: string | undefined | null,
  service: string | undefined | null,
  scope: MaintenanceScope) => {

  const sonarClient = useSonarApi();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: AdHocMaintenanceConfiguration) => {
      switch (scope) {
        case MaintenanceScope.Environment:
          return sonarClient.toggleAdhocEnvironmentMaintenance(env, body);
        case MaintenanceScope.Tenant:
          return sonarClient.toggleAdhocTenantMaintenance(env, tenant!, body);
        case MaintenanceScope.Service:
          return sonarClient.toggleAdhocServiceMaintenance(env, tenant!, service!, body);
      }
    },
    onSuccess: (res) => {
      queryClient.invalidateQueries({queryKey: [MaintenanceQueryKeys.GetAdhocMaintenances]});
    }
  });
}
