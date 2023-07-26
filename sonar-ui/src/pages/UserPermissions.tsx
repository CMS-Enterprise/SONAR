import { parentContainerStyle, pageTitleStyle } from 'App.Style';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import { Outlet, useParams } from 'react-router';
import { Link } from 'react-router-dom';
import { usePermissionConfigurationByEnvironment } from './UserPermissions.Hooks';

const UserPermissions = () => {
  const { environmentName } = useParams();
  const { data: permConfigByEnv } = usePermissionConfigurationByEnvironment();

  const handleAddPermission = () => console.log('TODO: Handle add permission.');

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
      <Outlet context={permConfigByEnv || {}}/>
    </section>
  );
};

export default UserPermissions;
