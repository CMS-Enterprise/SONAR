import { useTheme } from '@emotion/react';
import React from 'react';
import {
  DateTimeHealthStatusValueTuple,
  HealthStatus
} from 'api/data-contracts';
import { validateHealthCheckObj } from 'helpers/HealthCheckHelper';
import {
  StatusHistoryChecklistStyle,
  StatusHistoryDrawerSectionStyle,
  StatusHistoryDrawerSubsectionStyle
} from './StatusHistory.Style';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../../Badges/HealthStatusBadge.Style';
import { DynamicTextFontStyle } from '../../../App.Style';

const StatusHistoryHealthCheckList: React.FC<{
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> = ({ healthChecks }) => {
  const theme = useTheme();

  return (
    <div css={StatusHistoryChecklistStyle}>
      <b css={StatusHistoryDrawerSectionStyle}>Health Checks</b>
      {Object.keys(healthChecks).map((key, i) => {
        const healthCheckObj: DateTimeHealthStatusValueTuple = healthChecks[key];
        const displayComponent = (
          <div key={i} css={StatusHistoryDrawerSubsectionStyle}>
            <span>
              <HealthStatusBadge theme={theme} status={healthChecks[key][1] as HealthStatus} />
            </span>
            <span css={[getBadgeSpanStyle(theme), DynamicTextFontStyle]}>{key}</span>
          </div>
        );

        return validateHealthCheckObj(healthCheckObj, displayComponent);
      })}
    </div>
  );
}

export default StatusHistoryHealthCheckList;
