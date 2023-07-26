import {
  ReactNode,
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState
} from 'react';
import { useOktaAuth } from '@okta/okta-react';
import { toRelativeUrl } from '@okta/okta-auth-js';
import { Api as SonarApi } from 'api/sonar-api.generated';
import { apiUrl as baseUrl } from 'config';
import { QueryClient, QueryClientProvider } from 'react-query';
import { CurrentUserView } from 'api/data-contracts';

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

interface AppContextType {
  sonarApi: SonarApi;
  userContext: {
    userIsAuthenticated: boolean;
    userInfo?: CurrentUserView | null;
    logUserIn: () => void;
    logUserOut: () => void;
  };
}

const AppContext = createContext<AppContextType | null>(null);

export const useSonarApi = () => useContext(AppContext)!.sonarApi;

export const useUserContext = () => useContext(AppContext)!.userContext;

export default function AppContextProvider({ children }: { children: ReactNode }): JSX.Element {
  const [ userIsAuthenticated, setUserIsAuthenticated ] = useState<boolean>(false);
  const [ userInfo, setUserInfo ] = useState<CurrentUserView | null>(null);
  const { oktaAuth, authState } = useOktaAuth();

  const logUserIn = useCallback(() => {
    async function doLogin() {
      oktaAuth.setOriginalUri(toRelativeUrl(window.location.href, window.location.origin));
      await oktaAuth.signInWithRedirect();
    }

    doLogin();
  }, [oktaAuth]);

  const logUserOut = useCallback(() => {
    async function doLogout() {
      await oktaAuth.signOut();
    }

    doLogout();
  }, [oktaAuth]);

  useEffect(() => {
    if (authState?.isAuthenticated) {
      sonarApi.setSecurityData(oktaAuth.getIdToken());
      sonarApi.v2UserCreate()
        .then((response) => {
          setUserIsAuthenticated(true);
          setUserInfo(response.data);
        })
        .catch((error) => {
          console.error(error);
          sonarApi.setSecurityData(null);
        });
    } else {
      sonarApi.setSecurityData(null);
      setUserIsAuthenticated(false);
      setUserInfo(null);
    }
  }, [ oktaAuth, authState, authState?.isAuthenticated ]);

  const appContextValue: AppContextType = {
    sonarApi,
    userContext: {
      userIsAuthenticated,
      userInfo,
      logUserIn,
      logUserOut
    }
  }

  return (
    <QueryClientProvider client={queryClient}>
      <AppContext.Provider value={appContextValue}>
        {children}
      </AppContext.Provider>
    </QueryClientProvider>
  );
}
