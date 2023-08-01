import { pageTitleStyle } from 'App.Style';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import React from 'react';
import { Link } from 'react-router-dom';

const UserPermissionsHeader: React.FC<{
  environment: string,
  handleAddModalToggle: () => void
}> = ({environment, handleAddModalToggle}) => {
  return (
    <>
      <div className='ds-l-row ds-u-justify-content--start'>
        <div className='ds-l-col--auto'>
          <div css={pageTitleStyle}>
            <Link to="/user-permissions">User Permissions</Link>
            {environment && ` - ${environment}`}
          </div>
        </div>
      </div>
      <div className='ds-l-row ds-u-justify-content--end'>
        <div className='ds-l-col--auto'>
          <PrimaryActionButton onClick={handleAddModalToggle}>+ Add new Permission</PrimaryActionButton>
        </div>
      </div>
    </>
  )
}

export default UserPermissionsHeader;
