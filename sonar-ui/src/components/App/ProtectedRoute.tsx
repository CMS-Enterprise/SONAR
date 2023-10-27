import { Spinner } from '@cmsgov/design-system';
import { useUserContext } from 'components/AppContext/AppContextProvider';
import React, { useEffect } from 'react';
import { Navigate, Outlet } from 'react-router';
import { useAlertContext } from './AlertContextProvider';

export default function ProtectedRoute(): JSX.Element {
  const { userIsAuthenticated, userInfo, logUserIn } = useUserContext();
  const { createAlert } = useAlertContext();

  useEffect(() => {
    if (userIsAuthenticated === false) {
      logUserIn();
    }
  }, [ userIsAuthenticated, logUserIn ]);

  useEffect(() => {
    if (userInfo && !userInfo.isAdmin) {
      createAlert(
        "Unauthorized to view requested page.",
        "Please contact your administrator to update your permissions.",
        "error");
    }
  }, [userInfo, createAlert]);

  if (!userIsAuthenticated || !userInfo) {

    return (<><Spinner/></>);
  }

  if (!userInfo.isAdmin) {
    return (<Navigate to="/" replace/>)
  }

  return (<Outlet />);
}
