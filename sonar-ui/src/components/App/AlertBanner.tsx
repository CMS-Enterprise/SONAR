import { Alert } from '@cmsgov/design-system';
import { AlertVariation } from '@cmsgov/design-system/dist/types/Alert/Alert';
import React from 'react';
import { getAlertStyle } from './AlertBanner.Style';

const AlertBanner: React.FC<{
  alertHeading: string,
  alertText: string,
  variation: AlertVariation | undefined
}> = ({
  alertHeading,
  alertText,
  variation
}) => {
  return (
    <Alert
      css={getAlertStyle()}
      heading={alertHeading}
      variation={variation}
    >
      {alertText}
    </Alert>
  )
}

export default AlertBanner;
