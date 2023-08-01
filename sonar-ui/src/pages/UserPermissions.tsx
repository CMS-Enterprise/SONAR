import { Spinner } from '@cmsgov/design-system';
import React, { useState } from 'react';
import { Outlet, useParams } from 'react-router';
import { parentContainerStyle } from 'App.Style';
import {
  PermissionConfigurationByEnvironment,
  UsersByEmail,
  usePermissionConfigurationByEnvironment,
  useUsersByEmail
} from './UserPermissions.Hooks';
import ThemedModalDialog from 'components/Common/ThemedModalDialog';
import AddPermissionForm from 'components/UserPermissions/AddPermissionForm';
import UserPermissionsHeader from 'components/UserPermissions/UserPermissionsHeader';

export interface OutletContextType {
  permConfigByEnv: PermissionConfigurationByEnvironment;
  usersByEmail: UsersByEmail;
}

const UserPermissions = () => {
  const { environmentName } = useParams();
  const {
    data: permConfigByEnv,
    isLoading: permConfigByEnvIsLoading
  } = usePermissionConfigurationByEnvironment();
  const {
    data: usersByEmail,
    isLoading: usersByEmailIsLoading
  } = useUsersByEmail();

  const [openAdd, setOpenAdd] = useState<boolean>(false);

  const handleAddModalToggle = () => {
    setOpenAdd(!openAdd);
  }

  const context: OutletContextType = {
    permConfigByEnv: permConfigByEnv || {},
    usersByEmail: usersByEmail || {}
  }

  return (
    <section className='ds-l-container' css={parentContainerStyle}>
      <UserPermissionsHeader
        environment={environmentName!}
        handleAddModalToggle={handleAddModalToggle}
      />
      {
        permConfigByEnvIsLoading || usersByEmailIsLoading
          ? <Spinner />
          : <Outlet context={context}/>
      }
      { openAdd ? (
        <ThemedModalDialog
          heading={"Add User Permission"}
          onExit={handleAddModalToggle}
          onClose={handleAddModalToggle}
          actions={
            <AddPermissionForm
              handleAddModalToggle={handleAddModalToggle}
            />
          }
        />
      ) : null}
    </section>
  );
};

export default UserPermissions;
