import React, { useEffect, useState } from 'react';
import { Button } from '@cmsgov/design-system';
import { useOktaAuth } from '@okta/okta-react';
import { toRelativeUrl } from '@okta/okta-auth-js';
import { UserClaims, CustomUserClaims } from '@okta/okta-auth-js'
import { useSonarApi } from 'components/SonarApi/Provider';
import DropdownModal from './DropdownModal'
import * as styles from './Header.Style';

const LoginButton = () => {
  const sonarClient = useSonarApi();
  const { authState, oktaAuth } = useOktaAuth();
  const [userInfo, setUserInfo] = useState<UserClaims<CustomUserClaims> | null>(null);
  const [modal, setModalOpen] = useState(false);

  const login = async () => {
    oktaAuth.setOriginalUri(toRelativeUrl(window.location.href, window.location.origin));
    await oktaAuth.signInWithRedirect();
  }

  const toggleModal = () => {
    setModalOpen(!modal);
  };

  useEffect(() => {
    if (!authState || !authState.isAuthenticated) {
      // When user isn't authenticated, forget any user info
      setUserInfo(null);
    } else {
      oktaAuth.getUser().then((info) => {
        setUserInfo(info);
        sonarClient.v2UserCreate()
      }).catch((err) => {
        console.error(err);
      });
    }
  }, [authState, oktaAuth, sonarClient]);

  if (!authState) {
    return (
      <div>Loading...</div>
    );
  }

  return (
    <span>
      {(authState.isAuthenticated && userInfo) ? (
          <>
            <Button css={styles.ButtonStyles} size={'small'} onClick={toggleModal}>
              Welcome, {userInfo.given_name}
            </Button>
            <DropdownModal showModal={modal} setShowModal={setModalOpen} />
          </>
        ) :
        (
          <Button css={styles.ButtonStyles} size={'small'} onClick={login}>
            Login
          </Button>
        )}
    </span>
  )
}

export default LoginButton;
