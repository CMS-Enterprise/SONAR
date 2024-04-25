import React, { useState } from 'react';
import ThemedModalDialog from '../Common/ThemedModalDialog';
import {
  ActiveAdHocMaintenanceView,
  AdHocMaintenanceConfiguration
} from '../../api/data-contracts';
import AlertBanner from 'components/App/AlertBanner';
import SecondaryActionButton from 'components/Common/SecondaryActionButton';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import { useToggleAdhocMaintenance } from './Maintenance.Hooks';


const RemoveAdhocMaintenanceModal: React.FC<{
  maintenance: ActiveAdHocMaintenanceView,
  entityName: string,
  handleModalToggle: () => void
}> = ({ maintenance, entityName, handleModalToggle }) => {
  const [alertHeading, setAlertHeading] = useState("Remove Active Ad-hoc Maintenance");
  const [alertText, setAlertText] = useState(`Are you sure you want to remove the active ad-hoc maintenance` +
    ` for ${maintenance.scope} ${entityName}?`);

  const removeMaintenance = useToggleAdhocMaintenance(
    maintenance.environment!,
    maintenance.tenant,
    maintenance.service,
    maintenance.scope);

  const handleDelete = () => {
    const body: AdHocMaintenanceConfiguration = {
      isEnabled: false
    };
    removeMaintenance.mutate(body, {
      onSuccess: res => {
        handleModalToggle();
      },
      onError: res => {
        setAlertHeading("Error Removing Ad-hoc Maintenance");
        setAlertText("An error occurred while processing your request. Please try again.")
      }
    })
  }

  return  (
    <ThemedModalDialog
      onExit={handleModalToggle}
      closeButtonVariation={"solid"}
      heading={"Remove Ad hoc maintenance"}
      onClose={handleModalToggle}
    >
      <div className="ds-l-row">
        <div className="ds-l-col--12">
          <AlertBanner
            alertHeading={alertHeading}
            alertText={alertText}
            variation='warn'
          />
        </div>
      </div>
      <div className="ds-l-row ds-u-justify-content--end">
        <div className="ds-l-col--3 ds-u-margin-right--1">
          <SecondaryActionButton onClick={handleModalToggle}>
            Cancel
          </SecondaryActionButton>
        </div>
        <div className="ds-l-col--3 ds-u-margin-right--1">
          <PrimaryActionButton onClick={handleDelete}>
            Delete
          </PrimaryActionButton>
        </div>
      </div>
    </ThemedModalDialog>
  );
}

export default RemoveAdhocMaintenanceModal;
