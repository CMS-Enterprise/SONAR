import React from 'react';
import { useOktaAuth } from '@okta/okta-react';
import { Button } from '@cmsgov/design-system';
import { useNavigate } from 'react-router';
import * as styles from './Header.Style';
import LogoutIcon from 'components/Icons/LogoutIcon';
import KeyIcon from 'components/Icons/ApiKeyIcon';

const DropdownModal: React.FC<{
  showModal: boolean,
  setShowModal: (value: boolean) => void,
}> = ({ showModal, setShowModal }) => {

  const navigate = useNavigate();
  const { oktaAuth } = useOktaAuth();
  const logout = async () => oktaAuth.signOut();

  return (
    <>
      {showModal && (
        <div css={styles.DropdownModalStyle} onMouseLeave={() => setShowModal(false)}>
          <Button
            css={[styles.ButtonStyles, styles.IconButtonStyle]}
            size={'small'}
            onClick={() => navigate('/api-keys')}
          >
            <KeyIcon /> Manage your API Keys
          </Button>
          <div css={styles.DropdownLine} />
          <Button css={[styles.ButtonStyles, styles.IconButtonStyle]} size={'small'} onClick={logout}>
            <LogoutIcon /> Logout </Button>
        </div>
      )}
    </>
  );
}

export default DropdownModal;
