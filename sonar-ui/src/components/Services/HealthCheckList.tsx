import React from 'react';
import { Accordion } from '@cmsgov/design-system';

import { DateTimeHealthStatusValueTuple, HealthStatus, ServiceConfiguration } from 'api/data-contracts';
import HealthCheckListItem from 'components/Services/HealthCheckListItem';
import { validateHealthCheckObj } from 'helpers/HealthCheckHelper';

const HealthCheckList: React.FC<{
  environmentName: string,
  tenantName: string,
  service: ServiceConfiguration,
  healthCheckStatuses: Record<string, DateTimeHealthStatusValueTuple> | undefined
}> =
  ({ environmentName, tenantName, service, healthCheckStatuses }) => {
    if (service.healthChecks?.length) {
      return (
        <div className="ds-l-col">
          Health Checks:
          {service.healthChecks.map((healthCheck, i) => {
            const healthCheckStatus: DateTimeHealthStatusValueTuple =
              (healthCheckStatuses ?
                healthCheckStatuses[healthCheck.name] :
                null) ??
              [new Date().toString(), HealthStatus.Unknown];
            const displayComponent = (
              <div key={i}>
                <Accordion bordered>
                  <HealthCheckListItem
                    environmentName={environmentName}
                    tenantName={tenantName}
                    service={service}
                    healthCheck={healthCheck}
                    healthCheckStatus={healthCheckStatus[1]} />
                </Accordion>
              </div>
            );

            return validateHealthCheckObj(healthCheckStatus, displayComponent);
          })}
        </div>
      );
    } else {
      return <></>;
    }
  };

export default HealthCheckList;
