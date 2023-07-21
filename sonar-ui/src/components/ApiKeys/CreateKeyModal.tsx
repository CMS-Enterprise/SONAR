import { Dialog } from '@cmsgov/design-system';
import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import { useTheme } from '@emotion/react';
import React, { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import { createSonarClient } from '../../helpers/ApiHelper';
import CreateKeyForm from './CreateKeyForm';
import { getDialogStyle } from './KeyModal.Style';

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

const CreateKeyModal: React.FC<{
  handleModalToggle: () => void
}> = ({ handleModalToggle }) => {
  const theme = useTheme();
  const sonarClient = createSonarClient();
  const roles = [INITIAL_OPTION, ...permissions]
  // const [roles, setRoles] = useState<DropdownOptions[]>([INITIAL_OPTION, ...permissions]);
  const [selectedRole, setSelectedRole] = useState<DropdownValue>(0);
  const [selectedEnvironment, setSelectedEnvironment] = useState<DropdownValue>(0);
  const [selectedTenant, setSelectedTenant] = useState<DropdownValue>(0);

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

  return  (
    <Dialog
      onExit={handleModalToggle}
      closeButtonVariation={"solid"}
      heading={"Create API Key"}
      css={getDialogStyle(theme)}
      actions={
        <CreateKeyForm
          selectedRole={selectedRole}
          roles={roles}
          setSelectedRole={setSelectedRole}
          selectedEnvironment={selectedEnvironment}
          environmentOptions={environmentOptions}
          setSelectedEnvironment={setSelectedEnvironment}
          selectedTenant={selectedTenant}
          tenantOptions={tenantOptions}
          setSelectedTenant={setSelectedTenant}
          handleModalToggle={handleModalToggle}
        />
      }
      onClose={handleModalToggle}
    />
  );
}

export default CreateKeyModal;
