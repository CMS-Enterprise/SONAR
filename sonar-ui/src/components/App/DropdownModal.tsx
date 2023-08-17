import React from 'react';
import { useNavigate } from 'react-router';
import * as styles from './Header.Style';
import LogoutIcon from 'components/Icons/LogoutIcon';
import KeyIcon from 'components/Icons/ApiKeyIcon';
import PersonIcon from 'components/Icons/PersonIcon';
import GhostActionButton from 'components/Common/GhostActionButton';
import { useUserContext } from 'components/AppContext/AppContextProvider';

const DropdownModal: React.FC<{
  showModal: boolean,
  setShowModal: (value: boolean) => void,
}> = ({ showModal, setShowModal }) => {
  const navigate = useNavigate();
  const { logUserOut, userInfo } = useUserContext();

  return (
    <>
      {showModal && (
        <div css={styles.DropdownModalStyle} onMouseLeave={() => setShowModal(false)}>
          {userInfo?.isAdmin ? (
            <>
              <GhostActionButton onClick={() => navigate('/user-permissions')}>
                <PersonIcon /> User Permissions
              </GhostActionButton>
              <GhostActionButton onClick={() => navigate('/api-keys')}>
                <KeyIcon /> Manage your API Keys
              </GhostActionButton>
              <div css={styles.DropdownLine} />
            </>
          ): null}
          <GhostActionButton onClick={logUserOut}>
            <LogoutIcon /> Logout
          </GhostActionButton>
        </div>
      )}
    </>
  );
}

export default DropdownModal;
