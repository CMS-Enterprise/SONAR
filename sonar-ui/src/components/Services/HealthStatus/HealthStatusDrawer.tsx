import { Drawer } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { HealthCheckType, HealthStatus } from 'api/data-contracts';
import { DynamicTextFontStyle } from 'App.Style'
import HealthStatusBadge from 'components/Badges/HealthStatusBadge';
import React, { useContext } from 'react';
import { IHealthCheckDefinition } from 'types';
import { getDrawerSectionHeaderStyle, getDrawerStyle } from '../Drawer.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { useGetServiceHealthCheckData } from '../Services.Hooks';
import HealthMetricThresholds from './HealthMetricThresholds';
import HealthStatusDataTable from './HealthStatusDataTable';
import HealthStatusDataTimeSeriesChart from './HealthStatusDataTimeSeriesChart';

const HealthStatusDrawer: React.FC<{
  onCloseClick: () => void
}> = ({
  onCloseClick
}) => {
  const theme = useTheme();
  const context = useContext(ServiceOverviewContext)!;
  const serviceConfiguration = context.serviceConfiguration;
  const healthCheck = context.selectedHealthCheck!;
  const healthCheckStatus = context.serviceHierarchyHealth.healthChecks![healthCheck.name] ?
    context.serviceHierarchyHealth.healthChecks![healthCheck.name][1] as HealthStatus :
    null;
  const healthCheckDefinition = healthCheck.definition as IHealthCheckDefinition;
  const isMetricHealthCheck = [HealthCheckType.LokiMetric, HealthCheckType.PrometheusMetric].includes(healthCheck.type);
  const drawerHeading = `Health Checks`;
  const healthCheckData = useGetServiceHealthCheckData(
    healthCheck,
    context.environmentName,
    context.tenantName,
    serviceConfiguration.name,
    healthCheck.name);

  const timeSeriesData = (!healthCheckData || !healthCheckData.data) ? [] :
    healthCheckData.data.timeSeries.slice().reverse() as number[][]

  return (
    <Drawer css={getDrawerStyle} heading={drawerHeading} headingLevel="3" onCloseClick={onCloseClick}>
      {healthCheckStatus && (
        <>
          <div css={DynamicTextFontStyle}>
            <b>{healthCheck.name}&nbsp;</b>
            <HealthStatusBadge theme={theme} status={healthCheckStatus} />
          </div>

          <div>
            <p><b>Description: </b>{healthCheck.description}</p>
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
      )}
    </Drawer>
  );
}

export default HealthStatusDrawer;
