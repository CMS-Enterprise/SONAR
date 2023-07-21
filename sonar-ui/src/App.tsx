import { OktaAuth, toRelativeUrl } from '@okta/okta-auth-js';
import React, { useState, useCallback } from 'react';
import { ThemeProvider } from '@emotion/react';
import { QueryClient, QueryClientProvider } from 'react-query';
import { Routes, Route, useNavigate } from 'react-router-dom';
import { Security, LoginCallback } from '@okta/okta-react';
import { mainStyle } from './App.Style';
import Header from './components/App/Header';
import ApiKeys from './pages/ApiKeys';
import Environments from './pages/Environments';
import Service from './pages/Service';
import { LightTheme, DarkTheme } from './themes';
import { oktaAuthOptions } from './config';
import UserPermissions from 'pages/UserPermissions';
import EnvironmentUsersTable from 'components/UserPermissions/EnvironmentUsersTable';
import UserPermissionsTable from 'components/UserPermissions/UserPermissionsTable';

const queryClient = new QueryClient();

const oktaAuth = new OktaAuth(oktaAuthOptions);

function App() {
  const [enableDarkTheme, setEnableDarkTheme] = useState(false);

  const navigate = useNavigate();
  const restoreOriginalUri = useCallback((_oktaAuth: OktaAuth, originalUri: string) => {
    navigate(toRelativeUrl(originalUri || '/', window.location.origin));
  }, [navigate]);

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={enableDarkTheme ? DarkTheme : LightTheme}>
        <Security oktaAuth={oktaAuth} restoreOriginalUri={restoreOriginalUri}>
          <main css={mainStyle} data-test="app-main">
            <Header enableDarkTheme={enableDarkTheme} setEnableDarkTheme={setEnableDarkTheme} />
            <Routes>
              <Route path="/" element={<Environments />} />
              <Route path="/:environment/tenants/:tenant/services/*" element={<Service />} />
              <Route path="/login/callback" element={<LoginCallback />} />
              <Route path="/api-keys" element={<ApiKeys />} />
              <Route path="/user-permissions" element={<UserPermissions />}>
                <Route index={true} element={<EnvironmentUsersTable />} />
                <Route path="environments/:environmentName" element={<UserPermissionsTable />}/>
              </Route>
            </Routes>
          </main>
        </Security>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
