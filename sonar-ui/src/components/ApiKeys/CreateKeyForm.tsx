import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import { useOktaAuth } from '@okta/okta-react';
import React, { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { ApiKeyDetails, PermissionType } from '../../api/data-contracts';
import { createSonarClient } from '../../helpers/ApiHelper';
import AlertBanner from '../App/AlertBanner';
import PrimaryActionButton from '../Common/PrimaryActionButton';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import ThemedDropdown from '../Common/ThemedDropdown';
import ThemedTextField from '../Common/ThemedTextField';

const permissions: DropdownOptions[] = [
  {
    label: "Admin",
    value: "Admin"
  },
  {
    label: "Standard",
    value: "Standard"
  }
];

const INITIAL_OPTION: DropdownOptions = {
  label: "Please Select",
  value: 0
}

const INITIAL_ENV_OPTION: DropdownOptions = {
  label: "All Environments",
  value: 0
}

const INITIAL_TENANT_OPTION: DropdownOptions = {
  label: "All Tenants",
  value: 0
}

const CreateKeyForm: React.FC<{
  handleModalToggle: () => void
}> = ({
  handleModalToggle
}) => {
  const sonarClient = createSonarClient();
  const queryClient = useQueryClient();
  const { oktaAuth } = useOktaAuth();

  const roles = [INITIAL_OPTION, ...permissions]
  // const [roles, setRoles] = useState<DropdownOptions[]>([INITIAL_OPTION, ...permissions]);
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

  const environmentData = useQuery({
    queryKey: ['environments'],
    staleTime: Infinity,
    refetchOnMount: false,
    refetchOnWindowFocus: false,
    queryFn: () => sonarClient.getEnvironments()
      .then((res) => {
        return res.data;
      })
  });

  const environmentOptions = (!environmentData  || !environmentData.data) ? [] :
    [INITIAL_ENV_OPTION].concat(
      environmentData.data?.map((env) => {
        const option: DropdownOptions = {
          label: env.environmentName,
          value: env.environmentName
        }
        return option;
      })
    );

  const [tenantOptions, setTenantOptions] =
    useState<DropdownOptions[]>([INITIAL_OPTION]);
  const tenantData = useQuery({
    queryKey: ['tenants'],
    staleTime: Infinity,
    refetchOnMount: false,
    queryFn: () => sonarClient.getTenants()
      .then((res) => {
        return res.data;
      })
  });

  useEffect(() => {
    const allTenants = !tenantData.data ? [] : tenantData.data;
    if (+selectedEnvironment !== 0) {
      setTenantOptions(
        [INITIAL_TENANT_OPTION].concat(
          allTenants.filter(tenant => tenant.environmentName === selectedEnvironment)
            .map((tenant) => {
              const option: DropdownOptions = {
                label: tenant.tenantName,
                value: tenant.tenantName
              }
              return option;
            })
        )
      );
    } else {
      setTenantOptions([INITIAL_TENANT_OPTION]);
    }
    setSelectedTenant(0);
  }, [selectedEnvironment, tenantData.data]);

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
