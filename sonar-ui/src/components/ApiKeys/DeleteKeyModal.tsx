import React, { useState } from 'react';
import ThemedModalDialog from '../Common/ThemedModalDialog';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import AlertBanner from 'components/App/AlertBanner';
import SecondaryActionButton from 'components/Common/SecondaryActionButton';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import { useDeleteKey } from './ApiKeys.Hooks';


const DeleteKeyModal: React.FC<{
  apiKey: ApiKeyConfiguration,
  handleModalToggle: () => void
}> = ({ apiKey, handleModalToggle }) => {
  const [alertHeading, setAlertHeading] = useState("Deleting an API Key cannot be undone");
  const [alertText, setAlertText] = useState(`Are you sure you want to delete the ${apiKey.apiKeyType}` +
    ` API Key for the environment ${apiKey.environment} and tenant ${apiKey.tenant}?`);

  const deleteKey = useDeleteKey();

  const handleDelete = () => {
    deleteKey.mutate(apiKey.id!, {
      onSuccess: res => {
        handleModalToggle();
      },
      onError: res => {
        setAlertHeading("Error Deleting API Key");
        setAlertText("An error occurred while processing your request. Please try again.")
      }
    });
  }

  return  (
    <ThemedModalDialog
      onExit={handleModalToggle}
      closeButtonVariation={"solid"}
      heading={"Delete API Key"}
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

export default DeleteKeyModal;
