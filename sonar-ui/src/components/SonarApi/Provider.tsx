import {
  ReactNode,
  createContext,
  useContext,
  useEffect
} from 'react';
import { useOktaAuth } from '@okta/okta-react';
import { Api as SonarApi } from 'api/sonar-api.generated';
import { apiUrl as baseUrl } from 'config';
import { QueryClient, QueryClientProvider } from 'react-query';

const sonarApi = new SonarApi({
  baseUrl: baseUrl,
  baseApiParams: {
    secure: true
  },
  securityWorker: (idToken) => {
    return idToken
      ? {
          headers: {
            Authorization: `Bearer ${idToken}`
          }
        }
      : {}
  }
});

const queryClient = new QueryClient();
const SonarApiContext = createContext<SonarApi>(sonarApi);
export const useSonarApi = () => useContext(SonarApiContext);
export default function SonarApiProvider({ children }: { children: ReactNode }): JSX.Element {
  const { oktaAuth, authState } = useOktaAuth();

  useEffect(() => {
    if (authState?.isAuthenticated) {
      sonarApi.setSecurityData(oktaAuth.getIdToken());
    } else {
      sonarApi.setSecurityData(null);
    }
  }, [ oktaAuth, authState, authState?.isAuthenticated ]);

  return (
    <QueryClientProvider client={queryClient}>
      <SonarApiContext.Provider value={sonarApi}>
        {children}
      </SonarApiContext.Provider>
    </QueryClientProvider>
  );
}
