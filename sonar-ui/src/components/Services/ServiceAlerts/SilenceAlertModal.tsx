import React, { useContext, useState } from 'react';
import { AlertSilenceDetails, ServiceAlert } from '../../../api/data-contracts';
import AlertBanner from 'components/App/AlertBanner';
import SecondaryActionButton from 'components/Common/SecondaryActionButton';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import ThemedModalDialog from '../../Common/ThemedModalDialog';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { useCreateUpdateSilence, useRemoveSilence } from '../Services.Hooks';

const DeleteKeyModal: React.FC<{
  alert: ServiceAlert,
  handleModalToggle: () => void
}> = ({ alert, handleModalToggle }) => {

  const unsuccessfulUpdateHeading = "Error Processing Request";
  const [alertHeading, setAlertHeading] = useState(alert.isSilenced ?
  "Notifications for this Alert will resume." :
  "Notifications for this Alert will be silenced for 1 day.");
  const [alertText, setAlertText] = useState(alert.isSilenced ?
  `Are you sure you want to resume notifications for ${alert.name}?` :
  `Are you sure you want to silence ${alert.name}?`);

  const context = useContext(ServiceOverviewContext)!;
  const createUpdateSilence = useCreateUpdateSilence(
    context.environmentName,
    context.tenantName,
    context.serviceConfiguration.name);
  const removeSilence = useRemoveSilence(
    context.environmentName,
    context.tenantName,
    context.serviceConfiguration.name);

  const handleConfirmation = () => {
    const silenceDetails: AlertSilenceDetails = {
      name: alert.name
    };

    alert.isSilenced ?
      removeSilence.mutate(silenceDetails,{
        onSuccess: (res) => {
          handleModalToggle();
        },
        onError: res => {
          setAlertHeading(unsuccessfulUpdateHeading);
          setAlertText("An error occurred while processing your request. Please try again.")
        }
      }) :
      createUpdateSilence.mutate(silenceDetails,{
        onSuccess: (res) => {
          handleModalToggle();
        },
        onError: res => {
          setAlertHeading(unsuccessfulUpdateHeading);
          setAlertText("An error occurred while processing your request. Please try again.")
        }
      })
  }

  return  (
    <ThemedModalDialog
      onExit={handleModalToggle}
      closeButtonVariation={"solid"}
      heading={"Notification Preferences"}
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
          <PrimaryActionButton onClick={handleConfirmation}>
            {alert.isSilenced ? (
              <>Unsilence</>
            ) : (
              <>Silence</>
            )}
          </PrimaryActionButton>
        </div>
      </div>
    </ThemedModalDialog>
  );
}

export default DeleteKeyModal;
