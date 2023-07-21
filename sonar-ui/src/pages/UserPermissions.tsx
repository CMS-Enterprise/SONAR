import { parentContainerStyle, pageTitleStyle } from 'App.Style';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import { Outlet, useParams } from 'react-router';
import { Link } from 'react-router-dom';

const UserPermissions = () => {
  const params = useParams();

  const handleAddPermission = () => console.log('TODO: Handle add permission.');

  return (
    <section className='ds-l-container' css={parentContainerStyle}>
      <div className='ds-l-row ds-u-justify-content--start'>
        <div className='ds-l-col--auto'>
          <div css={pageTitleStyle}>
            <Link to="/user-permissions">User Permissions</Link>
            {params.environmentName && ` - ${params.environmentName}`}
          </div>
        </div>
      </div>
      <div className='ds-l-row ds-u-justify-content--end'>
        <div className='ds-l-col--auto'>
          <PrimaryActionButton onClick={handleAddPermission}>+ Add new Permission</PrimaryActionButton>
        </div>
      </div>
      <Outlet />
    </section>
  );
};

export default UserPermissions;
