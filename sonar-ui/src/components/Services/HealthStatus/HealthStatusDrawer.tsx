import React, { useContext, useState } from 'react';
import { Drawer } from '@cmsgov/design-system';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import HealthStatusBadge from 'components/Badges/HealthStatusBadge';
import { useTheme } from '@emotion/react';
import { HealthCheckType, HealthStatus } from 'api/data-contracts';
import HealthMetricThresholds from './HealthMetricThresholds';
import { createSonarClient } from 'helpers/ApiHelper';
import { useQuery } from 'react-query';
import HealthStatusDataTimeSeriesChart from './HealthStatusDataTimeSeriesChart';
import { IHealthCheckDefinition } from 'types';
import HealthStatusDataTable from './HealthStatusDataTable';
import {
  getDrawerSectionHeaderStyle,
  getDrawerStyle
} from '../Drawer.Style';
import { DynamicTextFontStyle } from 'App.Style'

const HealthStatusDrawer: React.FC<{
  onCloseClick: () => void
}> = ({
  onCloseClick
}) => {
  const theme = useTheme();
  const context = useContext(ServiceOverviewContext)!;

  const serviceConfiguration = context.serviceConfiguration;
  const healthCheck = context.selectedHealthCheck!;
  const healthCheckStatus = context.serviceHierarchyHealth.healthChecks![healthCheck.name][1] as HealthStatus;
  const healthCheckDefinition = healthCheck.definition as IHealthCheckDefinition;
  const isMetricHealthCheck = [HealthCheckType.LokiMetric, HealthCheckType.PrometheusMetric].includes(healthCheck.type);
  const drawerHeading = `Health Checks`;

  useQuery(
    `${healthCheck.name}-data`,
    () => createSonarClient()
      .getHealthCheckData(
        context.environmentName,
        context.tenantName,
        serviceConfiguration.name,
        healthCheck.name)
      .then(response => setTimeSeriesData(response.data.timeSeries.slice().reverse() as number[][]))
  );

  const [timeSeriesData, setTimeSeriesData] = useState<number[][]>([]);

  return (
    <Drawer css={getDrawerStyle} heading={drawerHeading} headingLevel="3" onCloseClick={onCloseClick}>
      <>
        <div css={DynamicTextFontStyle}>
          <b>{healthCheck.name}&nbsp;</b>
          <HealthStatusBadge theme={theme} status={healthCheckStatus} />
        </div>

        <h4 css={getDrawerSectionHeaderStyle}>Health Conditions&nbsp;-&nbsp;{healthCheck.type}</h4>
        <HealthMetricThresholds service={serviceConfiguration} healthCheck={healthCheck} healthCheckStatus={healthCheckStatus} />

        { isMetricHealthCheck && (
          <>
            <h4 css={getDrawerSectionHeaderStyle}>Health Check Metrics</h4>
            <HealthStatusDataTimeSeriesChart key={`${healthCheck.name}-ts`} svcDefinitions={healthCheckDefinition} healthCheckName={healthCheck.name} timeSeriesData={timeSeriesData} />
            <HealthStatusDataTable key={`${healthCheck.name}-dt`} healthCheckName={healthCheck.name} timeSeriesData={timeSeriesData} />
          </>
        )}
      </>
    </Drawer>
  );
}

export default HealthStatusDrawer;
