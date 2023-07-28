import { parentContainerStyle, pageTitleStyle } from 'App.Style';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import { Outlet, useParams } from 'react-router';
import { Link } from 'react-router-dom';
import {
  PermissionConfigurationByEnvironment,
  UsersByEmail,
  usePermissionConfigurationByEnvironment,
  useUsersByEmail
} from './UserPermissions.Hooks';
import { Spinner } from '@cmsgov/design-system';

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

  const handleAddPermission = () => console.log('TODO: Handle add permission.');

  const context: OutletContextType = {
    permConfigByEnv: permConfigByEnv || {},
    usersByEmail: usersByEmail || {}
  }

  return (
    <section className='ds-l-container' css={parentContainerStyle}>
      <div className='ds-l-row ds-u-justify-content--start'>
        <div className='ds-l-col--auto'>
          <div css={pageTitleStyle}>
            <Link to="/user-permissions">User Permissions</Link>
            {environmentName && ` - ${environmentName}`}
          </div>
        </div>
      </div>
      <div className='ds-l-row ds-u-justify-content--end'>
        <div className='ds-l-col--auto'>
          <PrimaryActionButton onClick={handleAddPermission}>+ Add new Permission</PrimaryActionButton>
        </div>
      </div>
      {
        permConfigByEnvIsLoading || usersByEmailIsLoading
          ? <Spinner />
          : <Outlet context={context}/>
      }
    </section>
  );
};

export default UserPermissions;
