import { useLocation, useNavigate, useOutletContext, useParams } from "react-router";
import { OutletContextType as UserPermissionsOutletContextType } from "pages/UserPermissions";
import ThemedModalDialog from "components/Common/ThemedModalDialog";
import SecondaryActionButton from "components/Common/SecondaryActionButton";
import PrimaryActionButton from "components/Common/PrimaryActionButton";
import { useDeletePermission } from 'pages/UserPermissions.Hooks';

const DeletePermissionModal = () => {
  const params = useParams();
  const location = useLocation();
  const context = useOutletContext<UserPermissionsOutletContextType>();
  const deletePermission = useDeletePermission();
  const navigate = useNavigate();

  const envPermConfigs = context.permConfigByEnv[params.environmentName!]
  const permConfig = envPermConfigs.filter((p) => p.id === params.permissionId).pop();
  const userName = permConfig?.userEmail && context.usersByEmail[permConfig?.userEmail];

  const closeModal = () => navigate(location.state.from, { replace: true });

  const handleDeletePermission = () => deletePermission.mutate(params.permissionId!, {
    onSuccess: closeModal
  });

  return (
    <ThemedModalDialog
      className='ds-c-dialog--wide'
      headerClassName="ds-u-display--none"
      backdropClickExits={true}
      onExit={closeModal}
    >
      <div className="ds-l-row ds-u-justify-content--center ds-u-padding-y--3">
          Are you sure you want to delete&nbsp;<b>{permConfig?.environment || 'Global'}</b>
          {
            permConfig?.tenant &&
              <b>/{permConfig?.tenant}</b>
          }
          &nbsp;<b>{permConfig?.permission}</b>&nbsp;permission for&nbsp;<b>{userName}</b>?
      </div>
      <div className="ds-l-row ds-u-justify-content--end">
        <div className='ds-u-padding-right--3'>
          <SecondaryActionButton onClick={closeModal}>Cancel</SecondaryActionButton>
        </div>
        <div>
          <PrimaryActionButton onClick={handleDeletePermission}>Delete</PrimaryActionButton>
        </div>
      </div>
    </ThemedModalDialog>
  );
}

export default DeletePermissionModal;
