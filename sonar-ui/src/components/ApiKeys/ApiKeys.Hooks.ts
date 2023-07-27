import { useMutation, useQuery, useQueryClient } from 'react-query';
import { ApiKeyDetails } from '../../api/data-contracts';
import { useSonarApi } from '../AppContext/AppContextProvider';

export enum QueryKeys {
  GetKeys = 'apiKeys',
  GetPermissions = 'permissions',
  GetTenantOptions = 'tenantOptions'
}

export const useGetKeys = () => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: QueryKeys.GetKeys,
    queryFn: () => sonarClient.v2KeysList()
      .then((res) => {
        return res.data
      })
  });
}

export const useGetPermissions = () => {
  const sonarClient = useSonarApi();
  return useQuery({
    queryKey: QueryKeys.GetPermissions,
    queryFn: () => sonarClient.getCurrentUser()
      .then((res) => {
        return res.data;
      })
  });
}

export const useCreateKey = () => {
  const sonarClient = useSonarApi();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (key: ApiKeyDetails) => sonarClient.v2KeysCreate(key),
    onSuccess: (res) => {
      queryClient.invalidateQueries({queryKey: [QueryKeys.GetKeys]});
    }
  });
}

export const useDeleteKey = () => {
  const sonarClient = useSonarApi();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => sonarClient.deleteApiKey(id),
    onSuccess: (res) => {
      queryClient.invalidateQueries({queryKey: [QueryKeys.GetKeys]});
    }
  });
}
