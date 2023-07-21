import React from 'react';
import { useOktaAuth } from '@okta/okta-react';
import { useNavigate } from 'react-router';
import * as styles from './Header.Style';
import LogoutIcon from 'components/Icons/LogoutIcon';
import KeyIcon from 'components/Icons/ApiKeyIcon';
import PersonIcon from 'components/Icons/PersonIcon';
import GhostActionButton from 'components/Common/GhostActionButton';

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
          <GhostActionButton onClick={() => navigate('/user-permissions')}>
            <PersonIcon /> User Permissions
          </GhostActionButton>
          <GhostActionButton onClick={() => navigate('/api-keys')}>
            <KeyIcon /> Manage your API Keys
          </GhostActionButton>
          <div css={styles.DropdownLine} />
          <GhostActionButton onClick={logout}>
            <LogoutIcon /> Logout
          </GhostActionButton>
        </div>
      )}
    </>
  );
}

export default DropdownModal;
