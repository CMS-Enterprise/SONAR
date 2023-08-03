import { Spinner } from '@cmsgov/design-system';
import React, { useEffect } from 'react';
import { Outlet } from "react-router";
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

  return (<Outlet />);
}
