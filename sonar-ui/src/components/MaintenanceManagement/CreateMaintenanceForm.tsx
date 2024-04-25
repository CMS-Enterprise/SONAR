import { DropdownOptions, DropdownValue } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import { useTheme } from '@emotion/react';
import { BaseOptions } from 'flatpickr/dist/types/options';
import React, { useEffect, useState } from 'react';
import Flatpickr from 'react-flatpickr';
import { AdHocMaintenanceConfiguration, MaintenanceScope } from '../../api/data-contracts';
import { HttpResponse } from '../../api/http-client';
import {
  getEnvironmentOptions,
  getServiceOptionsByTenant,
  getTenantOptions,
  initialServiceOption,
  initialTenantOption,
  requiredEnvOption
} from '../../helpers/DropdownOptions';
import AlertBanner from '../App/AlertBanner';
import PrimaryActionButton from '../Common/PrimaryActionButton';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import ThemedDropdown from '../Common/ThemedDropdown';
import { useGetEnvironmentsView, useGetTenantsView } from '../Environments/Environments.Hooks';
import { DatePickerContainerStyle, getDatePickerStyle } from './Maintenanace.Style';
import { useToggleAdhocMaintenance } from './Maintenance.Hooks';

function getDefaultDate() {
  const now = new Date();
  now.setHours(now.getHours() + 2);
  return now;
}

const CreateMaintenanceForm: React.FC<{
  handleModalToggle: () => void
}> = ({
  handleModalToggle
}) => {
  const theme = useTheme();
  const [selectedEnvironment, setSelectedEnvironment] = useState<DropdownValue>(0);
  const [selectedTenant, setSelectedTenant] = useState<DropdownValue>(0);
  const [selectedService, setSelectedService] = useState<DropdownValue>(0);
  const [selectedEndDate, setSelectedEndDate] = useState<Date>(getDefaultDate);
  const [currentScope, setCurrentScope] = useState<MaintenanceScope>(MaintenanceScope.Environment);
  const [submitDisabled, setSubmitDisabled] = useState(true);
  const [alertHeading, setAlertHeading] = useState("Environment Field is required.");
  const [alertText, setAlertText] = useState("Set remaining fields to create tenant or service-scoped maintenance.");

  const environmentData = useGetEnvironmentsView();
  const environmentOptions = (!environmentData  || !environmentData.data) ?
    [requiredEnvOption] : getEnvironmentOptions(environmentData.data);

  const [tenantOptions, setTenantOptions] =
    useState<DropdownOptions[]>([initialTenantOption]);
  const tenantData = useGetTenantsView(true);

  const [serviceOptions, setServiceOptions] = useState<DropdownOptions[]>([initialTenantOption])

  const toggleMaintenance = useToggleAdhocMaintenance(
    selectedEnvironment.toString(),
    selectedTenant.toString(),
    selectedService.toString(),
    currentScope);

  // hook to update the tenant options when the environment value is changed. Automatically
  // resets the selected tenant and service options to the default placeholder when
  // the environment is changed.
  useEffect(() => {
    const allTenants = !tenantData.data ? [] : tenantData.data;
    if (+selectedEnvironment !== 0) {
      setTenantOptions(
        [initialTenantOption].concat(getTenantOptions(allTenants, selectedEnvironment))
      );
    } else {
      setTenantOptions([initialTenantOption]);
      setServiceOptions([initialTenantOption]);
    }
    setSelectedTenant(0);
    setSelectedService(0);
  }, [selectedEnvironment, tenantData.data]);

  // hook to update service options when the tenant value is changed. Automatically
  // resets the selected service option to the default placeholder when the tenant is changed.
  useEffect(() => {
    const allTenants = !tenantData.data ? [] : tenantData.data;
    if (+selectedTenant !== 0) {
      setServiceOptions(
        [initialServiceOption].concat(getServiceOptionsByTenant(allTenants, selectedEnvironment, selectedTenant))
      );
    } else {
      setServiceOptions([initialServiceOption]);
    }
    setSelectedService(0);
  }, [selectedEnvironment, selectedTenant, tenantData.data]);

  // hook to set the current scope based on the selected env/tenant/service options.
  // triggers when any 3 of the inputs are updated.
  useEffect(() => {
    let currScope;
    if (+selectedEnvironment !== 0 && +selectedTenant !== 0 && +selectedService !== 0) {
      currScope = MaintenanceScope.Service;
    } else if (+selectedEnvironment !== 0 && +selectedTenant !== 0) {
      currScope = MaintenanceScope.Tenant;
    } else {
      currScope = MaintenanceScope.Environment;
    }
    setCurrentScope(currScope);
  }, [selectedEnvironment, selectedService, selectedTenant]);

  const handleSubmit = () => {
    const newMaintenance: AdHocMaintenanceConfiguration = {
      isEnabled: true
    }

    // add end date if defined
    if (selectedEndDate) {
      newMaintenance.endTime = selectedEndDate.toISOString();
    }

    toggleMaintenance.mutate(newMaintenance, {
      onSuccess: res => {
        handleModalToggle();
      },
      onError: (err) => {
        // set error state
        if ((err as HttpResponse<Error>).status === 409) {
          setAlertHeading("Active ad-hoc maintenance window already exists.");
          setAlertText("An active ad-hoc maintenance window for this scope already exists. Only one maintenance per scope can be active at a time.");
        } else {
          setAlertHeading("Error Toggling Ad-hoc Maintenance");
          setAlertText("An error occurred while processing your request. Please try again.");
        }
      }
    });
  }

  // hook to update disabled state of submit button
  useEffect(() => {
    if (+selectedEnvironment === 0) {
      setSubmitDisabled(true)
    } else {
      setSubmitDisabled(false)
    }
  }, [selectedEnvironment, selectedTenant, selectedService]);

  const getPickerOptions = (): Partial<BaseOptions> => {
    const currentDate = new Date();
    return {
      dateFormat: "n/j/Y h:i:S K",
      defaultHour: currentDate.getHours(),
      defaultMinute: currentDate.getMinutes(),
      static: true,
      minDate: currentDate,
      position: 'auto'
    }
  }

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
          css={DatePickerContainerStyle}
        >
          <label className='ds-c-label'>Maintenance Window End Date:</label>
          <Flatpickr
            css={getDatePickerStyle(theme)}
            placeholder={"Maintenance Window End Time"}
            data-enable-time
            value={selectedEndDate}
            onChange={([selectedDate]) => setSelectedEndDate(selectedDate)}
            options={getPickerOptions()}
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
            onChange={(event) => setSelectedTenant(event.target.value)}
            value={selectedTenant}
            options={tenantOptions}
          />
        </div>
      </div>
      <div className="ds-l-row">
        <div
          className="ds-l-col--8"
        >
          <ThemedDropdown
            label="Service:"
            name="service_field"
            onChange={(event) => setSelectedService(event.target.value)}
            value={selectedService}
            options={serviceOptions}
          />
        </div>
      </div>
      <div className="ds-l-row">
        <div
          className="ds-l-col--12 ds-u-padding-right--0"
        >
          <AlertBanner
            alertHeading={alertHeading}
            alertText={alertText}
            variation={toggleMaintenance.isError ?
              "error" : "warn"}
          />
        </div>
      </div>
      <div className="ds-l-row ds-u-justify-content--end">
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
      </div>
    </section>
  )
}

export default CreateMaintenanceForm;
