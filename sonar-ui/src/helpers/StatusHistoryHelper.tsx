import { CheckIcon, CloseIconThin, RemoveIcon, WarningIcon } from '@cmsgov/design-system';
import React from 'react';
import { HealthStatus } from '../api/data-contracts';
import { StatusStartTimes } from 'helpers/DropdownOptions';

const secondsPerDay = 86400; // 60sec/min * 60min/h * 24h/day

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

export const getQueryRangeParameters = (
  startDate: Date,
  endDate: Date,
  stepInSeconds: number
) => {
  const start = startDate.toISOString();
  const end = endDate.toISOString();

  return { start, end, step: stepInSeconds };
}

export const convertUtcTimestampToLocal = (utcTimestamp: string, showDate: boolean) => {
  const localTimestamp = new Date(utcTimestamp);
  const localDateTimestamp = localTimestamp.toLocaleString();
  const localTimestampTime = localTimestamp.toLocaleTimeString();

  return showDate ? localDateTimestamp : localTimestampTime;
}

export const getStartDate = (timeRangeOption: StatusStartTimes, currentDate: Date) => {
  const currentDateTime = currentDate.getTime();
  const millisecondsPerHour = 3600000; // 60min * 60sec/min * 1000 ms/sec

  let startDate;
  switch (timeRangeOption) {
    case StatusStartTimes.Last10Hours:
      startDate = new Date(currentDateTime - 10 * millisecondsPerHour);
      break;
    case StatusStartTimes.Last12Hours:
      startDate = new Date(currentDateTime - 12 * millisecondsPerHour);
      break;
    case StatusStartTimes.Last24Hours:
      startDate = new Date(currentDateTime - 24 * millisecondsPerHour);
      break;
    case StatusStartTimes.Last48Hours:
      startDate = new Date(currentDateTime - 48 * millisecondsPerHour);
      break;
    case StatusStartTimes.LastWeek:
      startDate = new Date(currentDateTime - 7 * 24 * millisecondsPerHour);
      break;
    default: // Initial default start date is 6 hours ago
      startDate = new Date(currentDateTime - 6 * millisecondsPerHour);
      break;
  }

  return startDate;
}

export const calculateQueryStep = (
  rangeInMsec: number
) => {
  const numberOfTiles = 20;
  const differenceInSeconds = rangeInMsec / 1000;
  const queryStepInSec = differenceInSeconds / numberOfTiles;
  return queryStepInSec;
}

export const getNewRangeBasedDate = (
  dateToIncrement: Date,
  rangeInMsec: number
) => {
  return new Date(dateToIncrement.getTime() + rangeInMsec);
}

export const getTimeDiffAsString = (
  timeDiffInSec: number
) => {
  const secondsPerHour = 3600;

  const days = Math.floor(timeDiffInSec / secondsPerDay);
  const daysRemainder = timeDiffInSec % secondsPerDay;
  const hours = Math.floor(daysRemainder / secondsPerHour);
  const hoursRemainder = daysRemainder % secondsPerHour;
  const minutes = Math.floor(hoursRemainder / 60);
  const minutesRemainder = hoursRemainder % 60;

  let timeDiffAsString = '';
  if (days >= 1) {
    timeDiffAsString += days + 'd';
  }
  if (hours >= 1) {
    timeDiffAsString += hours + 'h';
  }
  if (minutes >= 1) {
    timeDiffAsString += minutes + 'm';
  }
  if (minutesRemainder > 0) {
    timeDiffAsString += minutesRemainder + 's';
  }

  return timeDiffAsString;
}
