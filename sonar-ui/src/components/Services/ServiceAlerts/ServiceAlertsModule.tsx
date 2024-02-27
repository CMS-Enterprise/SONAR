import React, { useContext } from 'react';
import { CollapsibleHeaderStyle } from '../ServiceOverview.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { useGetServiceAlerts } from '../Services.Hooks';
import { ServiceAlertsContainerStyle } from './ServiceAlerts.Style';
import ServiceAlertsTable from './ServiceAlertsTable';

const ServiceAlertsModule: React.FC = () => {
  const context = useContext(ServiceOverviewContext)!;
  const serviceAlertData = useGetServiceAlerts(
    context.environmentName,
    context.tenantName,
    context.serviceConfiguration.name);

  const numFiring = serviceAlertData.data?.filter(a => a.isFiring).length || 0;
  return (
    <>
      <details  css={CollapsibleHeaderStyle} open={numFiring > 0}>
        <summary>
          Alerts ({numFiring} firing)
        </summary>
        <div css={ServiceAlertsContainerStyle}>
          <ServiceAlertsTable alerts={serviceAlertData.data ? serviceAlertData.data : []} />
        </div>
      </details>
    </>
  );
};

export default ServiceAlertsModule;
