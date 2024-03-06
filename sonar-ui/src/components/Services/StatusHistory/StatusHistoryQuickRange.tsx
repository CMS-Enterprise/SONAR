import React from 'react';
import ThemedTextField from '../../Common/ThemedTextField';
import { StatusHistoryQuickRangeTextFieldStyle } from './StatusHistory.Style';

const StatusHistoryQuickRange: React.FC<{
  statusStartDate: Date,
  statusEndDate: Date
}> = ({
  statusStartDate,
  statusEndDate
}) => {
  const startDateLocalString =
    statusStartDate.toLocaleDateString() + ' ' + statusStartDate.toLocaleTimeString();
  const endDateLocalString =
    statusEndDate.toLocaleDateString() + ' ' + statusEndDate.toLocaleTimeString();

  return (
    <div>
      <ThemedTextField
        name="status-history-start-date"
        label={undefined}
        ariaLabel="status history start date"
        disabled
        value={startDateLocalString}
        css={StatusHistoryQuickRangeTextFieldStyle}
      />
      <span>to</span>
      <ThemedTextField
        name="status-history-end-date"
        label={undefined}
        ariaLabel="status history end date"
        disabled
        value={endDateLocalString}
        css={StatusHistoryQuickRangeTextFieldStyle}
      />
    </div>
  )
}

export default StatusHistoryQuickRange;
