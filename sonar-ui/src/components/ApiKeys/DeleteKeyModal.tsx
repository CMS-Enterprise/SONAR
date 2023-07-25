import { useOktaAuth } from '@okta/okta-react';
import React, { useState } from 'react';
import { useMutation, useQueryClient } from 'react-query';
import { useSonarApi } from 'components/SonarApi/Provider';
import ThemedModalDialog from '../Common/ThemedModalDialog';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import AlertBanner from 'components/App/AlertBanner';
import SecondaryActionButton from 'components/Common/SecondaryActionButton';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';


const DeleteKeyModal: React.FC<{
  apiKey: ApiKeyConfiguration,
  handleModalToggle: () => void
}> = ({ apiKey, handleModalToggle }) => {
  const sonarClient = useSonarApi();
  const queryClient = useQueryClient();
  const { oktaAuth } = useOktaAuth();
  const [alertHeading, setAlertHeading] = useState("Deleting an API Key cannot be undone");
  const [alertText, setAlertText] = useState(`Are you sure you want to delete the ${apiKey.apiKeyType}` +
    ` API Key for the environment ${apiKey.environment} and tenant ${apiKey.tenant}?`);

  const deleteKey = useMutation({
    mutationFn: () => sonarClient.deleteApiKey(apiKey.id!, {
      headers: {
        'Authorization': `bearer ${oktaAuth.getIdToken()}`
      }
    }),
    onSuccess: res => {
      handleModalToggle();
      queryClient.invalidateQueries({queryKey: ['apiKeys']});
    },
    onError: res => {
      setAlertHeading("Error Deleting API Key");
      setAlertText("An error occurred while processing your request. Please try again.")
    }
  });

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
          <PrimaryActionButton onClick={() => deleteKey.mutate()}>
            Delete
          </PrimaryActionButton>
        </div>
      </div>
    </ThemedModalDialog>
  );
}

export default DeleteKeyModal;
