import { useTheme } from '@emotion/react';
import { Drawer, Spinner } from '@cmsgov/design-system';
import React, { useContext } from 'react';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { useGetHealthCheckResultForService } from '../Services.Hooks';
import StatusHistoryHealthCheckList from './StatusHistoryHealthCheckList';
import { StatusHistoryDrawerSubsectionStyle } from './StatusHistory.Style';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanNoLinkStyle } from '../../Badges/HealthStatusBadge.Style';
import { HealthStatus } from '../../../api/data-contracts';
import { DynamicTextFontStyle } from '../../../App.Style';
import { getDrawerStyle } from '../Drawer.Style';
import { convertUtcTimestampToLocal } from '../../../helpers/StatusHistoryHelper';

const StatusHistoryDrawer: React.FC<{
  statusHistoryViewData: StatusHistoryView | null,
  closeDrawer: () => void,
  showDate: boolean
}> = ({ statusHistoryViewData, closeDrawer, showDate}) => {
  const theme = useTheme();
  const context = useContext(ServiceOverviewContext)!;
  const utcDateTimestamp = statusHistoryViewData?.statusTimestampTuple[0];
  const convertedTimestamp = convertUtcTimestampToLocal(utcDateTimestamp!, showDate);
  const dateTimestampStatus = statusHistoryViewData?.statusTimestampTuple[1] as HealthStatus;
  const {isLoading, data} = useGetHealthCheckResultForService(
    context.environmentName,
    context.tenantName,
    statusHistoryViewData?.serviceData.name as string,
    utcDateTimestamp as string
  );

  return (
    <Drawer css={getDrawerStyle} heading={"Selected Timestamps"} headingLevel="3" onCloseClick={closeDrawer}>
      {statusHistoryViewData && (
        <>
          <div css={DynamicTextFontStyle}>
            <b>{statusHistoryViewData.serviceData.name}</b>
          </div>
          <div css={StatusHistoryDrawerSubsectionStyle}>
            <span>
              <HealthStatusBadge theme={theme} status={dateTimestampStatus} />
            </span>
            <span css={[getBadgeSpanNoLinkStyle(theme), DynamicTextFontStyle]}>{convertedTimestamp}</span>
          </div>
          {
            isLoading ? (<Spinner />) :
              data ? (<StatusHistoryHealthCheckList healthChecks={data} />) :
                null
          }
        </>

      )}
    </Drawer>
  )
}

export default StatusHistoryDrawer;
