import React from 'react';
import { getTimeDiffAsString } from 'helpers/StatusHistoryHelper';

const StatusHistoryRangeInfo: React.FC<{
  isCustomRange: boolean,
  rangeInMsec: number,
  queryStepInSec: number
}> = ({
  isCustomRange,
  rangeInMsec,
  queryStepInSec
}) => {
  let rangeStepInfo = '';

  if (isCustomRange) {
    rangeStepInfo += 'range: ' + getTimeDiffAsString(rangeInMsec / 1000) + ', ';
  }

  rangeStepInfo += 'step: ' + getTimeDiffAsString(queryStepInSec);

  return (
    <div>
      <span>{rangeStepInfo}</span>
    </div>
  )
}

export default StatusHistoryRangeInfo;
