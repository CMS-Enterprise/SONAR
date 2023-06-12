import { useTheme } from '@emotion/react';
import React from 'react';
import {
  DateTimeHealthStatusValueTuple,
  HealthStatus
} from 'api/data-contracts';
import { validateHealthCheckObj } from 'helpers/HealthCheckHelper';
import {
  StatusHistoryChecklistStyle,
  StatusHistoryDrawerSubsectionStyle
} from './StatusHistory.Style';
import { getDrawerSectionHeaderStyle } from '../Drawer.Style';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanNoLinkStyle } from '../../Badges/HealthStatusBadge.Style';
import { DynamicTextFontStyle } from '../../../App.Style';

const StatusHistoryHealthCheckList: React.FC<{
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> = ({ healthChecks }) => {
  const theme = useTheme();

  return (
    <div css={StatusHistoryChecklistStyle}>
      <h4 css={getDrawerSectionHeaderStyle}>Health Checks</h4>
      {Object.keys(healthChecks).map((key, i) => {
        const healthCheckObj: DateTimeHealthStatusValueTuple = healthChecks[key];
        const displayComponent = (
          <div key={i} css={StatusHistoryDrawerSubsectionStyle}>
            <span>
              <HealthStatusBadge theme={theme} status={healthChecks[key][1] as HealthStatus} />
            </span>
            <span css={[getBadgeSpanNoLinkStyle(theme), DynamicTextFontStyle]}>{key}</span>
          </div>
        );

        return validateHealthCheckObj(healthCheckObj, displayComponent);
      })}
    </div>
  );
}

export default StatusHistoryHealthCheckList;
