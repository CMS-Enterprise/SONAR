import React from 'react';
import { ServiceVersionTypeInfo } from 'api/data-contracts';
import {
  StatusHistoryChecklistStyle,
  StatusHistoryDrawerSubsectionStyle
} from './StatusHistory.Style';
import { getDrawerSectionHeaderStyle } from '../Drawer.Style';

const StatusHistoryVersionAtTimestamp: React.FC<{
  versionAtTimestamp: ServiceVersionTypeInfo[]
}> = ({
  versionAtTimestamp
}) => {
  return (
    <div css={StatusHistoryChecklistStyle}>
      <h4 css={getDrawerSectionHeaderStyle}>
        Version at Timestamp
      </h4>
      { (versionAtTimestamp.length > 0) ?
        versionAtTimestamp.map(function(data) {
          return (
            <div css={StatusHistoryDrawerSubsectionStyle}>
              {data.versionType} : {data.version}
            </div>
          )}
        ) :
        <div css={StatusHistoryDrawerSubsectionStyle}>
          No version recorded for service in current status history range (as of selected timestamp).
        </div>
      }
    </div>
  );
}

export default StatusHistoryVersionAtTimestamp;
