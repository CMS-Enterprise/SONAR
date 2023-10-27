import { Alert, CloseIconThin } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import GhostActionButton from '../Common/GhostActionButton';
import { useAlertContext } from './AlertContextProvider';
import { AlertBannerCloseIconStyle } from './AppAlertBanner.Style';

const AppAlertBanner = () => {
  const theme = useTheme();
  const { alerts, deleteAlertById } = useAlertContext();

  return (
    <>
      {alerts.length > 0 ? alerts.map(alert =>
        (
          <div key={alert.id} css={{marginBottom: 10}}>
            <Alert
              heading={alert.alertHeader}
              variation={alert.alertType}
            >
              {alert.alertBody}
              <GhostActionButton onClick={() => deleteAlertById(alert.id)} css={AlertBannerCloseIconStyle(theme)}>
                <CloseIconThin />
              </GhostActionButton>
            </Alert>
          </div>

        )) : null}
    </>

  )
}

export default AppAlertBanner;
