import { Dialog } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { useOktaAuth } from '@okta/okta-react';
import React, { useState } from 'react';
import { useMutation, useQueryClient } from 'react-query';
import { createSonarClient } from '../../helpers/ApiHelper';
import { getDialogStyle } from './KeyModal.Style';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import AlertBanner from 'components/App/AlertBanner';
import SecondaryActionButton from 'components/Common/SecondaryActionButton';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';


const DeleteKeyModal: React.FC<{
  apiKey: ApiKeyConfiguration,
  handleModalToggle: () => void
}> = ({ apiKey, handleModalToggle }) => {
  const theme = useTheme();
  const sonarClient = createSonarClient();
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
    <Dialog
      onExit={handleModalToggle}
      closeButtonVariation={"solid"}
      heading={"Delete API Key"}
      css={getDialogStyle(theme)}
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
    </Dialog>
  );
}

export default DeleteKeyModal;
