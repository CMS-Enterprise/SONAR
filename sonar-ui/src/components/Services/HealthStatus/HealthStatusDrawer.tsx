import { Drawer } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { HealthCheckType, HealthStatus } from 'api/data-contracts';
import { DynamicTextFontStyle } from 'App.Style'
import HealthStatusBadge from 'components/Badges/HealthStatusBadge';
import React, { useContext } from 'react';
import { getDrawerStyle } from '../Drawer.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import ArgoHealthCheckDetails from './HealthCheckDetails/ArgoHealthCheckDetails';
import MetricHealthCheckDetails from './HealthCheckDetails/MetricHealthCheckDetails';

const HealthStatusDrawer: React.FC<{
  onCloseClick: () => void
}> = ({
  onCloseClick
}) => {
  const theme = useTheme();
  const drawerHeading = `Health Check Details`;
  const context = useContext(ServiceOverviewContext)!;

  const healthCheck = context.selectedHealthCheck!;
  const healthCheckStatus = context.serviceHierarchyHealth.healthChecks![healthCheck.name] ?
    context.serviceHierarchyHealth.healthChecks![healthCheck.name][1] as HealthStatus :
    null;
  
  let healthCheckDetailsComponent;

  switch (healthCheck.type) {
    case HealthCheckType.HttpRequest:
    case HealthCheckType.LokiMetric:
    case HealthCheckType.PrometheusMetric:
      healthCheckDetailsComponent = <MetricHealthCheckDetails />;
      break;
    case HealthCheckType.ArgoCd:
      healthCheckDetailsComponent = <ArgoHealthCheckDetails healthCheckStatus={healthCheckStatus} />;
      break
    default:
      healthCheckDetailsComponent = null;
  }

  return (
    <Drawer css={getDrawerStyle} heading={drawerHeading} headingLevel="3" onCloseClick={onCloseClick}>
      {healthCheckStatus ? (
        <>
          <div css={DynamicTextFontStyle}>
            <b>{healthCheck.name}&nbsp;</b>
            <HealthStatusBadge theme={theme} status={healthCheckStatus} />
          </div>

          {healthCheck.description && (
            <div>
              <p><b>Description: </b>{healthCheck.description}</p>
            </div>
          )}

          {healthCheckDetailsComponent}
        </>
      ) : (
        <div css={DynamicTextFontStyle}>
          <b>No status recorded for health check: {healthCheck.name}</b>
        </div>
      )}
    </Drawer>
  );
}

export default HealthStatusDrawer;
