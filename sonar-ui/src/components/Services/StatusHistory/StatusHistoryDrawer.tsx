import { useTheme } from '@emotion/react';
import { Drawer } from '@cmsgov/design-system';
import React from 'react';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import StatusHistoryHealthCheckList from './StatusHistoryHealthCheckList';
import {
  StatusHistoryDrawerSectionStyle,
  StatusHistoryDrawerSubsectionStyle
} from './StatusHistory.Style';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanNoLinkStyle } from '../../Badges/HealthStatusBadge.Style';
import { HealthStatus } from '../../../api/data-contracts';
import { DynamicTextFontStyle } from '../../../App.Style';
import { getDrawerStyle } from '../Drawer.Style';
import { convertUtcTimestampToLocal } from '../../../helpers/StatusHistoryHelper';

const StatusHistoryDrawer: React.FC<{
  statusHistoryViewData: StatusHistoryView | null,
  closeDrawer: () => void,
  environment: string,
  tenant: string,
  showDate: boolean
}> = ({ statusHistoryViewData, closeDrawer, environment, tenant , showDate}) => {
  const theme = useTheme();

  const utcDateTimestamp = statusHistoryViewData?.statusTimestampTuple[0];
  const convertedTimestamp = convertUtcTimestampToLocal(utcDateTimestamp!, showDate);
  const dateTimestampStatus = statusHistoryViewData?.statusTimestampTuple[1] as HealthStatus;

  return (
    <Drawer css={getDrawerStyle} heading={"Selected Timestamps"} headingLevel="3" onCloseClick={closeDrawer}>
      {statusHistoryViewData && (
        <>
          <div css={DynamicTextFontStyle}>
            <b css={StatusHistoryDrawerSectionStyle}>{statusHistoryViewData.serviceData.name}</b>
          </div>
          <div css={StatusHistoryDrawerSubsectionStyle}>
            <span>
              <HealthStatusBadge theme={theme} status={dateTimestampStatus} />
            </span>
            <span css={[getBadgeSpanNoLinkStyle(theme), DynamicTextFontStyle]}>{convertedTimestamp}</span>
          </div>
          {statusHistoryViewData.serviceData.healthChecks ? (
            <StatusHistoryHealthCheckList healthChecks={statusHistoryViewData.serviceData.healthChecks} />) :
            null}
        </>

      )}
    </Drawer>
  )
}

export default StatusHistoryDrawer;
