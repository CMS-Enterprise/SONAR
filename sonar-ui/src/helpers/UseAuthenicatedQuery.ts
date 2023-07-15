import { useOktaAuth } from '@okta/okta-react';
import { QueryKey, useQuery, UseQueryOptions } from 'react-query';

function useAuthenticatedQuery<
  TQueryFnData,
  TError,
  TQueryKey extends QueryKey = QueryKey,
  TData = TQueryFnData>(
  queryKey: TQueryKey,
  fetcher: (params: TQueryKey, token: string) => Promise<TQueryFnData>,
  options?: Omit<
    UseQueryOptions<TQueryFnData, TError, TData, TQueryKey>,
    'queryKey' | 'queryFn'
  >
) {

  const { oktaAuth } = useOktaAuth();
  return useQuery({
    queryKey,
    queryFn: async () => {
      const token = oktaAuth.getIdToken();
      return fetcher(queryKey, token!);
    },
    ...options
  });
}

export default useAuthenticatedQuery;
