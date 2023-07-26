import React, { useState } from 'react';
import { Button } from '@cmsgov/design-system';
import DropdownModal from './DropdownModal'
import * as styles from './Header.Style';
import { useUserContext } from 'components/AppContext/AppContextProvider';

const LoginButton = () => {
  const { userIsAuthenticated, userInfo, logUserIn } = useUserContext();
  const [ modalIsOpen, setModalIsOpen ] = useState(false);

  const toggleModal = () => setModalIsOpen(!modalIsOpen);

  return (
    <span>
      { userIsAuthenticated
        ?
          <>
            <Button css={styles.ButtonStyles} size={'small'} onClick={toggleModal}>
              Welcome, {userInfo?.fullName}
            </Button>
            <DropdownModal showModal={modalIsOpen} setShowModal={setModalIsOpen} />
          </>
        :
          <Button css={styles.ButtonStyles} size={'small'} onClick={logUserIn}>
            Login
          </Button>
      }
    </span>
  )
}

export default LoginButton;
