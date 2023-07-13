import { OktaAuth, toRelativeUrl } from '@okta/okta-auth-js';
import React, { useCallback, useMemo, useState } from 'react';
import { ThemeProvider } from '@emotion/react';
import { QueryClient, QueryClientProvider } from 'react-query';
import {
  BrowserRouter as Router, Routes, Route
} from 'react-router-dom';
import { Security, LoginCallback } from '@okta/okta-react';
import { mainStyle } from './App.Style';
import Header from './components/App/Header';
import Environments from './pages/Environments';
import Service from './pages/Service';
import { LightTheme, DarkTheme } from './themes';

const queryClient = new QueryClient();

function App() {
  const [enableDarkTheme, setEnableDarkTheme] = useState(false);

  /* eslint-disable  @typescript-eslint/no-explicit-any */
  const oktaIssuer = (window as any).OKTA_ISSUER;
  const oktaClientId = (window as any).OKTA_CLIENTID;
  /* eslint-enable  @typescript-eslint/no-explicit-any */

  const oktaAuth = useMemo(
    () => new OktaAuth({
      //issuer: 'https://dev-50063805.okta.com/oauth2/default',
      issuer: oktaIssuer,
      clientId: oktaClientId,
      redirectUri: window.location.origin + '/login/callback'
    }),
    [oktaIssuer, oktaClientId]
  );

  const restoreOriginalUri = useCallback(
    async (_oktaAuth: OktaAuth, originalUri: string) => {
      window.location.replace(
        toRelativeUrl(originalUri || '/', window.location.origin)
      );
    },
    []
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={enableDarkTheme ? DarkTheme : LightTheme}>
        <Router>
          <main css={mainStyle} data-test="app-main">
            <div>
              <Security oktaAuth={oktaAuth} restoreOriginalUri={restoreOriginalUri}>
                <Header enableDarkTheme={enableDarkTheme} setEnableDarkTheme={setEnableDarkTheme} />
                <Routes>
                  <Route path="/" element={<Environments />} />
                  <Route path="/:environment/tenants/:tenant/services/*" element={<Service />} />
                  <Route path="/login/callback" element={<LoginCallback />} />
                </Routes>
              </Security>
            </div>
          </main>
        </Router>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
