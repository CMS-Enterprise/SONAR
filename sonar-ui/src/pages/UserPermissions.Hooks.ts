import { UseQueryResult, useMutation, useQuery, useQueryClient } from "react-query";
import {
  CurrentUserView,
  PermissionConfiguration,
  PermissionDetails
} from "api/data-contracts";
import { useSonarApi } from "components/AppContext/AppContextProvider";

export enum QueryKeys {
  GetPermissions = 'getPermissions',
  GetUsers = 'getUsers'
}

export type PermissionConfigurationByEnvironment = {
  [key: string]: PermissionConfiguration[];
}

export type UsersByEmail = {
  [key: string]: string
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
        return (response.data || []).reduce(groupByEnvironment, {});
      })
  });
}

function toUsersByEmail(usersByEmail: UsersByEmail, user: CurrentUserView) {
  usersByEmail[user.email!] = user.fullName!
  return usersByEmail;
}

export function useUsersByEmail(): UseQueryResult<UsersByEmail> {
  const sonarApi = useSonarApi();
  return useQuery({
    queryKey: [QueryKeys.GetUsers],
    queryFn: () => sonarApi.v2UserList()
      .then((response) => {
        return (response.data || []).reduce(toUsersByEmail, {});
      })
  });
}

export function useAddPermission() {
  const sonarApi = useSonarApi();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (newPermission: PermissionDetails) => sonarApi.v2PermissionsCreate(newPermission),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QueryKeys.GetPermissions] })
  });
}

export function useDeletePermission() {
  const sonarApi = useSonarApi();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (permId: string) => sonarApi.deleteUserPermission(permId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [QueryKeys.GetPermissions] })
  });
}
