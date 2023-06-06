import { useTheme } from '@emotion/react';
import React from 'react';

import {
  DateTimeHealthStatusValueTuple,
  HealthStatus,
  ServiceConfiguration } from 'api/data-contracts';
import { StaticTextFontStyle, DynamicTextFontStyle } from '../../App.Style';
import HealthStatusBadge from '../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../Badges/HealthStatusBadge.Style';
import {
  getSubContainerStyle,
  getSubsectionContainerStyle,
  ServiceOverviewHeaderStyle
} from './ServiceOverview.Style';

const HealthCheckList: React.FC<{
  environmentName: string,
  tenantName: string,
  serviceConfig: ServiceConfiguration,
  healthCheckStatuses: Record<string, DateTimeHealthStatusValueTuple> | undefined
}> =
  ({ environmentName, tenantName, serviceConfig, healthCheckStatuses }) => {
    const theme = useTheme();

    if (serviceConfig.healthChecks?.length) {
      return (
        <>
          <div css={[ServiceOverviewHeaderStyle, StaticTextFontStyle]}>
            Health Checks
          </div>
          {serviceConfig.healthChecks.map((healthCheck, i) => {
            const healthCheckStatusData: DateTimeHealthStatusValueTuple =
              (healthCheckStatuses ?
                healthCheckStatuses[healthCheck.name] :
                null) ??
              [new Date().toString(), HealthStatus.Unknown];

            const displayComponent = (
              <div key={i} css={getSubContainerStyle}>
                <div css={[getSubsectionContainerStyle, DynamicTextFontStyle]}>
                  <span>
                    <HealthStatusBadge theme={theme} status={healthCheckStatusData[1] as HealthStatus} />
                  </span>
                  <span css={getBadgeSpanStyle}>{healthCheck.name}</span>
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
