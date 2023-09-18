import { useTheme } from '@emotion/react';
import React, { useContext } from 'react';
import { DateTimeHealthStatusValueTuple,  HealthStatus, } from 'api/data-contracts';
import { DynamicTextFontStyle } from '../../../App.Style';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../../Badges/HealthStatusBadge.Style';
import {
  getSubContainerStyle,
  getSubsectionContainerStyle,
  ServiceOverviewHeaderStyle
} from '../ServiceOverview.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';

const HealthCheckList: React.FC<{
  healthCheckStatuses: Record<string, DateTimeHealthStatusValueTuple> | undefined
}> =
  ({ healthCheckStatuses }) => {
    const theme = useTheme();
    const context = useContext(ServiceOverviewContext)!;
    const serviceConfiguration = context.serviceConfiguration;

    if (serviceConfiguration.healthChecks?.length) {
      return (
        <>
          <div css={ServiceOverviewHeaderStyle}>
            Health Checks
          </div>
          {serviceConfiguration.healthChecks.map((healthCheck, i) => {
            const healthCheckStatusData: DateTimeHealthStatusValueTuple =
              (healthCheckStatuses ?
                healthCheckStatuses[healthCheck.name] :
                null) ??
              [new Date().toString(), HealthStatus.Unknown];

            const displayComponent = (
              <div key={i} css={getSubContainerStyle} onClick={e => context.setSelectedHealthCheck(healthCheck)}>
                <div css={[getSubsectionContainerStyle, DynamicTextFontStyle]}>
                  <span>
                    <HealthStatusBadge theme={theme} status={healthCheckStatusData[1] as HealthStatus} />
                  </span>
                  <span
                    css={getBadgeSpanStyle}
                    data-test="health-check-name"
                  >{healthCheck.name}</span>
                </div>
              </div>
            );

            return displayComponent;
          })}
        </>
      );
    } else {
      return <></>;
    }
  };

export default HealthCheckList;
