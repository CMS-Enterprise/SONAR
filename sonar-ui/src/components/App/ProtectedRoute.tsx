import { Spinner } from '@cmsgov/design-system';
import React, { useEffect } from 'react';
import { Navigate, Outlet } from 'react-router';
import { useUserContext } from "components/AppContext/AppContextProvider";

export default function ProtectedRoute(): JSX.Element {
  const { userIsAuthenticated, userInfo, logUserIn } = useUserContext();

  useEffect(() => {
    if (userIsAuthenticated === false) {
      logUserIn();
    }
  }, [ userIsAuthenticated, logUserIn ]);

  if (!userIsAuthenticated || !userInfo) {
    return (<><Spinner/></>);
  }

  if (!userInfo.isAdmin) {
    return (<Navigate to="/" replace/>)
  }

  return (<Outlet />);
}
