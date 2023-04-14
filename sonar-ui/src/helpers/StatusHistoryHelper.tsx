import { CheckIcon, CloseIconThin, RemoveIcon, WarningIcon } from '@cmsgov/design-system';
import React from 'react';
import { HealthStatus } from '../api/data-contracts';

export const renderStatusIcon = (status: HealthStatus) => {
  let result: React.ReactNode;
  switch (status) {
    case HealthStatus.Online:
      result = (<CheckIcon />);
      break;
    case HealthStatus.AtRisk:
    case HealthStatus.Degraded:
      result = (<WarningIcon />);
      break;
    case HealthStatus.Unknown:
      result = (<RemoveIcon />);
      break;
    default:
      result = (<CloseIconThin />);
  }
  return result;
}
