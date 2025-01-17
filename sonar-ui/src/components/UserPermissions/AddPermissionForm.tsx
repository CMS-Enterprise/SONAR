import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import React, { useEffect, useState } from 'react';
import { PermissionDetails, PermissionType } from '../../api/data-contracts';
import AlertBanner from '../App/AlertBanner';
import {
  getUserOptions,
  initialEnvOption,
  initialTenantOption,
  roles
} from '../../helpers/DropdownOptions';
import { useUserContext } from '../AppContext/AppContextProvider';
import ThemedDropdown from '../Common/ThemedDropdown';
import PrimaryActionButton from '../Common/PrimaryActionButton';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import {
  useAddPermission,
  useGetPermissionTree,
  useUsersByEmail
} from '../../pages/UserPermissions.Hooks';

const AddPermissionForm: React.FC<{
  handleAddModalToggle: () => void
}> = ({
  handleAddModalToggle
}) => {
  const {userInfo } = useUserContext();

  const addPermission = useAddPermission();
  const [selectedUserEmail, setSelectedUserEmail] = useState<DropdownValue>(0);
  const [selectedRole, setSelectedRole] = useState<DropdownValue>(0);
  const [selectedEnvironment, setSelectedEnvironment] = useState<DropdownValue>(0);
  const [selectedTenant, setSelectedTenant] = useState<DropdownValue>(0);
  const [submitDisabled, setSubmitDisabled] = useState(true);
  const [alertHeading, setAlertHeading] = useState("All fields are required");
  const [alertText, setAlertText] = useState("Set all fields to add a new user permission.");
  const [permissionCreated, setPermissionCreated] = useState(false);

  const permissionTreeData = useGetPermissionTree();

  const usersByEmail = useUsersByEmail();
  const userOptions = getUserOptions(usersByEmail.data || {}, userInfo?.email ? userInfo.email : '');

  const [environmentOptions, setEnvironmentOptions] =
    useState<DropdownOptions[]>([initialEnvOption]);

  const [tenantOptions, setTenantOptions] =
    useState<DropdownOptions[]>([initialTenantOption]);

  useEffect(() => {
    if (+selectedUserEmail !== 0) {
      setEnvironmentOptions(
        [initialEnvOption].concat(!permissionTreeData.data?.permissionTree ? [] : Object.keys(permissionTreeData.data.permissionTree).map(
          env => {
            const option: DropdownOptions = {
              label: env,
              value: env
            }
            return option;
          }))
      )
    } else {
      setEnvironmentOptions([initialEnvOption]);
      setTenantOptions([initialTenantOption]);
    }
    setSelectedEnvironment(0);
    setSelectedTenant(0);
  }, [permissionTreeData.data, selectedUserEmail])

  useEffect(() => {
    if (+selectedEnvironment !== 0) {
      setTenantOptions(
        [initialTenantOption].concat(
          !permissionTreeData.data?.permissionTree ? [] :
            permissionTreeData.data.permissionTree[selectedEnvironment].map(tenant => {
              const option: DropdownOptions = {
                label: tenant,
                value: tenant
              }
              return option;
            })
        )
      );
    }
  }, [permissionTreeData.data?.permissionTree, selectedEnvironment])

  const handleSubmit = () => {
    const newPermission: PermissionDetails = {
      permission: PermissionType[selectedRole.toString() as keyof typeof PermissionType],
      userEmail: selectedUserEmail.toString(),
      environment: +selectedEnvironment === 0 ? null : selectedEnvironment.toString(),
      tenant: +selectedTenant === 0 ? null : selectedTenant.toString()
    }

    addPermission.mutate(newPermission, {
      onSuccess: (res) => {
        setAlertHeading("User Permission Successfully Added");
        setAlertText("Please click Close.")
        setPermissionCreated(true);
      },
      onError: (res) => {
        // set error state
        setAlertHeading("Error Adding Permission");
        setAlertText("An error occurred while processing your request. Please try again.")
      }
    });
  }

  // hook to update disabled state of submit button
  useEffect(() => {
    if ((+selectedUserEmail === 0) || (+selectedRole === 0)) {
      setSubmitDisabled(true)
    } else {
      setSubmitDisabled(false)
    }
  }, [selectedUserEmail, selectedRole]);

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        <div className="ds-l-col--12">
          <ThemedDropdown
            label="User:"
            name="user_field"
            disabled={permissionCreated}
            onChange={(event) => setSelectedUserEmail(event.target.value)}
            value={selectedUserEmail}
            options={userOptions}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div className="ds-l-col--6">
          <ThemedDropdown
            label="Role:"
            name="role_field"
            disabled={permissionCreated}
            onChange={(event) => setSelectedRole(event.target.value)}
            value={selectedRole}
            options={roles}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div className="ds-l-col--8">
          <ThemedDropdown
            label="Environment:"
            name="environment_field"
            disabled={permissionCreated}
            onChange={(event) => setSelectedEnvironment(event.target.value)}
            value={selectedEnvironment}
            options={environmentOptions}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
        >
          <ThemedDropdown
            label="Tenant:"
            name="tenant_field"
            disabled={permissionCreated}
            onChange={(event) => setSelectedTenant(event.target.value)}
            value={selectedTenant}
            options={tenantOptions}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div
          className="ds-l-col--12 ds-u-padding-right--0"
        >
          {permissionCreated ? (
            <AlertBanner
              alertHeading={alertHeading}
              alertText={alertText}
              variation={"success"}
            />
          ) : (
            <AlertBanner
              alertHeading={alertHeading}
              alertText={alertText}
              variation={addPermission.isError ?
                "error" : permissionCreated ?
                  "warn" : undefined}
            />
          )}
        </div>
      </div>

      <div className="ds-l-row ds-u-justify-content--end">
        {permissionCreated ? (
          <PrimaryActionButton
            onClick={handleAddModalToggle}
          >
            Close
          </PrimaryActionButton>
        ) : (
          <>
            <div
              className="ds-l-col--3 ds-u-margin-right--1"
            >
              <SecondaryActionButton
                onClick={handleAddModalToggle}
              >
                Cancel
              </SecondaryActionButton>
            </div>
            <div
              className="ds-l-col--3"
            >
              <PrimaryActionButton
                onClick={handleSubmit}
                disabled={submitDisabled}
              >
                Add
              </PrimaryActionButton>
            </div>
          </>
        )}
      </div>
    </section>
  )
}

export default AddPermissionForm;
