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

export const calculateHistoryRange = () => {
  // calculate start and end dates statically for now.
  const dateObj = new Date();
  const end = dateObj.toISOString();
  (dateObj.setHours(dateObj.getHours() - 12));
  const start = dateObj.toISOString();
  const stepSeconds = 2160;

  return { start, end, step: stepSeconds };
}

export const convertUtcTimestampToLocal = (utcTimestamp: string, showDate: boolean) => {
  const localTimestamp = new Date(utcTimestamp);
  const localDateTimestamp = localTimestamp.toLocaleString();
  const localTimestampTime = localTimestamp.toLocaleTimeString();

  return showDate ? localDateTimestamp : localTimestampTime;
}
