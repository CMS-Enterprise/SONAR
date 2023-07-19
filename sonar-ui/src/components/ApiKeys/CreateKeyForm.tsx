import { Dropdown, TextField } from '@cmsgov/design-system';
import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import { useTheme } from '@emotion/react';
import { useOktaAuth } from '@okta/okta-react';
import React, { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from 'react-query';
import { ApiKeyDetails, PermissionType } from '../../api/data-contracts';
import { createSonarClient } from '../../helpers/ApiHelper';
import AlertBanner from '../App/AlertBanner';
import PrimaryActionButton from '../Common/PrimaryActionButton';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import { getInputLabelStyle } from './KeyModal.Style';

const CreateKeyForm: React.FC<{
  selectedRole: DropdownValue,
  roles: DropdownOptions[],
  setSelectedRole: (value: DropdownValue) => void,
  selectedEnvironment: DropdownValue,
  environmentOptions: DropdownOptions[],
  setSelectedEnvironment: (value: DropdownValue) => void,
  selectedTenant: DropdownValue,
  tenantOptions: DropdownOptions[],
  setSelectedTenant: (value: DropdownValue) => void,
  handleModalToggle: () => void
}> = ({
  selectedRole,
  roles,
  setSelectedRole,
  selectedEnvironment,
  environmentOptions,
  setSelectedEnvironment,
  selectedTenant,
  tenantOptions,
  setSelectedTenant,
  handleModalToggle
}) => {
  const theme = useTheme();
  const sonarClient = createSonarClient();
  const queryClient = useQueryClient();
  const { oktaAuth } = useOktaAuth();

  const [submitDisabled, setSubmitDisabled] = useState(true);
  const [createdKeyId, setCreatedKeyId] = useState("");
  const [createdKeyVal, setCreatedKeyVal] = useState("");
  const [copied, setCopied] = useState(false);
  const [alertHeading, setAlertHeading] = useState("All fields are required");
  const [alertText, setAlertText] = useState("Set all fields to add a new user permission.");
  const [keyCreated, setKeyCreated] = useState(false);

  const mutation = useMutation({
    mutationFn: (newKey: ApiKeyDetails) => sonarClient.v2KeysCreate(newKey, {
      headers: {
        'Authorization': `Bearer ${oktaAuth.getIdToken()}`
      }
    }),
    onSuccess: res => {
      // request was successful, invalidate apiKeys query to re-fetch in background.
      // update state accordingly
      queryClient.invalidateQueries({queryKey: ['apiKeys']});
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

  const handleSubmit = () => {
    const newKey: ApiKeyDetails = {
      apiKeyType: PermissionType[selectedRole.toString() as keyof typeof PermissionType],
      environment: +selectedEnvironment === 0 ? null : selectedEnvironment.toString(),
      tenant: +selectedTenant === 0 ? null : selectedTenant.toString()
    }
    mutation.mutate(newKey)
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
          <Dropdown
            label="Role:"
            name="role_field"
            disabled={keyCreated}
            onChange={(event) => setSelectedRole(event.target.value)}
            value={selectedRole}
            options={roles}
            css={getInputLabelStyle(theme)}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
        >
          <Dropdown
            label="Environment:"
            name="environment_field"
            disabled={keyCreated}
            onChange={(event) => setSelectedEnvironment(event.target.value)}
            value={selectedEnvironment}
            options={environmentOptions}
            css={getInputLabelStyle(theme)}
          />
        </div>
      </div>

      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
        >
          <Dropdown
            label="Tenant:"
            name="tenant_field"
            disabled={keyCreated}
            onChange={(event) => setSelectedTenant(event.target.value)}
            value={selectedTenant}
            options={tenantOptions}
            css={getInputLabelStyle(theme)}
          />
        </div>
      </div>
      {keyCreated ? (
        <>
          <div className="ds-l-row ds-u-align-items--end">
            <div
              className="ds-l-col--8"
            >
              <TextField
                name="key-id-field"
                label="ID:"
                readOnly
                value={createdKeyId}
              />
            </div>
          </div>
          <div className="ds-l-row ds-u-align-items--end">
            <div
              className="ds-l-col--8"
            >
              <TextField
                name="key-text-field"
                label="API KEY:"
                readOnly
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
          className="ds-l-col--12"
        >
          <AlertBanner
            alertHeading={alertHeading}
            alertText={alertText}
            variation={mutation.isError ?
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
              className="ds-l-col--3 ds-u-margin-right--1"
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
