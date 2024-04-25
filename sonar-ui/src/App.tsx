import { OktaAuth, toRelativeUrl } from '@okta/okta-auth-js';
import React, { useState, useCallback } from 'react';
import { ThemeProvider } from '@emotion/react';
import { Routes, Route, useNavigate } from 'react-router-dom';
import { Security, LoginCallback } from '@okta/okta-react';
import { mainStyle } from './App.Style';
import AlertContextProvider from './components/App/AlertContextProvider';
import AppAlertBanner from './components/App/AppAlertBanner';
import Header from './components/App/Header';
import Environment from './pages/Environment';
import ApiKeys from './pages/ApiKeys';
import Environments from './pages/Environments';
import MaintenanceManagement from './pages/MaintenanceManagement';
import Service from './pages/Service';
import Tenant from './pages/Tenant';
import ErrorReports from './pages/ErrorReports';
import ErrorReportsForTenant from './pages/ErrorReportsForTenant';
import { LightTheme, DarkTheme } from './themes';
import { oktaAuthOptions } from './config';
import UserPermissions from 'pages/UserPermissions';
import EnvironmentUsersTable from 'components/UserPermissions/EnvironmentUsersTable';
import UserPermissionsTable from 'components/UserPermissions/UserPermissionsTable';
import AppContextProvider from 'components/AppContext/AppContextProvider';
import DeletePermissionModal from 'components/UserPermissions/DeletePermissionModal';
import ProtectedRoute from 'components/App/ProtectedRoute';

const oktaAuth = new OktaAuth(oktaAuthOptions);

function App() {
  const [enableDarkTheme, setEnableDarkTheme] = useState(false);

  const navigate = useNavigate();
  const restoreOriginalUri = useCallback((_oktaAuth: OktaAuth, originalUri: string) => {
    navigate(toRelativeUrl(originalUri || '/', window.location.origin));
  }, [navigate]);

  return (
    <ThemeProvider theme={enableDarkTheme ? DarkTheme : LightTheme}>
      <Security oktaAuth={oktaAuth} restoreOriginalUri={restoreOriginalUri}>
        <AppContextProvider>
          <AlertContextProvider>
            <main css={mainStyle} data-test="app-main">
              <AppAlertBanner />
              <Header enableDarkTheme={enableDarkTheme} setEnableDarkTheme={setEnableDarkTheme} />

              <Routes>
                <Route path="/" element={<Environments />} />
                <Route path="/:environment" element={<Environment />} />
                <Route path="/:environment/tenants/:tenant" element={<Tenant />} />
                <Route path="/:environment/tenants/:tenant/services/*" element={<Service />} />
                <Route path="/login/callback" element={<LoginCallback />} />
                <Route path="/api-keys" element={<ProtectedRoute />}>
                  <Route path="" element={<ApiKeys />} />
                </Route>
                <Route path="/user-permissions" element={<ProtectedRoute />}>
                  <Route path="" element={<UserPermissions />}>
                    <Route index={true} element={<EnvironmentUsersTable />} />
                    <Route path="environments/:environmentName" element={<UserPermissionsTable />}>
                      <Route path=":permissionId/delete" element={<DeletePermissionModal />}/>
                    </Route>
                  </Route>
                </Route>
                <Route path="/maintenance-management" element={<ProtectedRoute />}>
                  <Route path="" element={<MaintenanceManagement />} />
                </Route>
                <Route path="/error-reports" element={<ProtectedRoute />}>
                  <Route path="environments/:environment" element={<ErrorReports />}/>
                  <Route path="environments/:environment/tenants/:tenant" element={<ErrorReportsForTenant />}/>
                  <Route path="environments/:environment/tenants/:tenant/services/*" element={<ErrorReportsForTenant />}/>
                </Route>
              </Routes>
            </main>
          </AlertContextProvider>
        </AppContextProvider>
      </Security>
    </ThemeProvider>
  );
}

export default App;
