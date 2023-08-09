import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import React, { useEffect, useState } from 'react';
import { ApiKeyDetails, PermissionType } from '../../api/data-contracts';
import { useCreateKey } from './ApiKeys.Hooks';
import AlertBanner from '../App/AlertBanner';
import {
  getEnvironmentOptions,
  getTenantOptions,
  initialEnvOption,
  initialTenantOption,
  roles
} from '../../helpers/DropdownOptions';
import PrimaryActionButton from '../Common/PrimaryActionButton';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import ThemedDropdown from '../Common/ThemedDropdown';
import ThemedTextField from '../Common/ThemedTextField';
import { useGetEnvironments, useGetTenants } from '../Environments/Environments.Hooks';

const CreateKeyForm: React.FC<{
  handleModalToggle: () => void
}> = ({
  handleModalToggle
}) => {
  const createKey = useCreateKey();
  const [selectedRole, setSelectedRole] = useState<DropdownValue>(0);
  const [selectedEnvironment, setSelectedEnvironment] = useState<DropdownValue>(0);
  const [selectedTenant, setSelectedTenant] = useState<DropdownValue>(0);
  const [submitDisabled, setSubmitDisabled] = useState(true);
  const [createdKeyId, setCreatedKeyId] = useState("");
  const [createdKeyVal, setCreatedKeyVal] = useState("");
  const [copied, setCopied] = useState(false);
  const [alertHeading, setAlertHeading] = useState("All fields are required");
  const [alertText, setAlertText] = useState("Set all fields to add a new user permission.");
  const [keyCreated, setKeyCreated] = useState(false);

  const environmentData = useGetEnvironments();
  const environmentOptions = (!environmentData  || !environmentData.data) ?
    [initialEnvOption] : getEnvironmentOptions(environmentData.data);

  const [tenantOptions, setTenantOptions] =
    useState<DropdownOptions[]>([initialTenantOption]);
  const tenantData = useGetTenants(true);

  useEffect(() => {
    const allTenants = !tenantData.data ? [] : tenantData.data;
    if (+selectedEnvironment !== 0) {
      setTenantOptions(
        [initialTenantOption].concat(getTenantOptions(allTenants, selectedEnvironment))
      );
    } else {
      setTenantOptions([initialTenantOption]);
    }
    setSelectedTenant(0);
  }, [selectedEnvironment, tenantData.data]);

  const handleSubmit = () => {
    const newKey: ApiKeyDetails = {
      apiKeyType: PermissionType[selectedRole.toString() as keyof typeof PermissionType],
      environment: +selectedEnvironment === 0 ? null : selectedEnvironment.toString(),
      tenant: +selectedTenant === 0 ? null : selectedTenant.toString()
    };

    createKey.mutate(newKey, {
      onSuccess: (res) => {
        setCreatedKeyId(res.data.id ? res.data.id : "");
        setCreatedKeyVal(res.data.apiKey);
        setAlertHeading("Copy your API Key to a safe location");
        setAlertText("Once you click Close, you will not be able to access it.")
        setKeyCreated(true);
      },
      onError: res => {
        // set error state
        setAlertHeading("Error Creating API Key");
        setAlertText("An error occurred while processing your request. Please try again.")
      }
    });
  }

  // hook to update disabled state of submit button
  useEffect(() => {
    if (+selectedRole === 0) {
      setSubmitDisabled(true)
    } else {
      setSubmitDisabled(false)
    }
  }, [selectedRole, selectedEnvironment, selectedTenant]);

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        <div
          className="ds-l-col--6"
        >
          <ThemedDropdown
            label="Role:"
            name="role_field"
            disabled={keyCreated}
            onChange={(event) => setSelectedRole(event.target.value)}
            value={selectedRole}
            options={roles}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
        >
          <ThemedDropdown
            label="Environment:"
            name="environment_field"
            disabled={keyCreated}
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
            disabled={keyCreated}
            onChange={(event) => setSelectedTenant(event.target.value)}
            value={selectedTenant}
            options={tenantOptions}
          />
        </div>
      </div>
      {keyCreated ? (
        <>
          <div className="ds-l-row ds-u-align-items--end">
            <div
              className="ds-l-col--8"
            >
              <ThemedTextField
                name="key-id-field"
                label="ID:"
                disabled
                value={createdKeyId}
              />
            </div>
          </div>
          <div className="ds-l-row ds-u-align-items--end">
            <div
              className="ds-l-col--8"
            >
              <ThemedTextField
                name="key-text-field"
                label="API KEY:"
                disabled
                value={createdKeyVal}
              />
              <PrimaryActionButton
                onClick={() => {
                  navigator.clipboard.writeText(createdKeyVal);
                  setCopied(true)
                }}
                size="small"
              >
                {copied ? "Copied" : "Copy"}
              </PrimaryActionButton>
            </div>
          </div>
        </>
        ) : null}

      <div className="ds-l-row">
        <div
          className="ds-l-col--12 ds-u-padding-right--0"
        >
          <AlertBanner
            alertHeading={alertHeading}
            alertText={alertText}
            variation={createKey.isError ?
              "error" : keyCreated ?
                "warn" : undefined}
          />
        </div>
      </div>

      <div className="ds-l-row ds-u-justify-content--end">
        {keyCreated ? (
          <PrimaryActionButton
            onClick={handleModalToggle}
          >
            Close
          </PrimaryActionButton>
        ) : (
          <>
            <div
              className="ds-l-col--3 ds-u-margin-right--1"
            >
              <SecondaryActionButton
                onClick={handleModalToggle}
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
                Create
              </PrimaryActionButton>
            </div>
          </>
        )}
      </div>
    </section>
  )
}

export default CreateKeyForm;
