import React from 'react';
import { Accordion } from '@cmsgov/design-system';

import { DateTimeHealthStatusValueTuple, HealthStatus } from 'api/data-contracts';
import HealthCheckListItem from 'components/ServiceListView/HealthCheckListItem';

const HealthCheckList: React.FC<{
  environmentName: string,
  tenantName: string,
  rootServiceName?: string | null,
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> =
  ({ environmentName, tenantName, rootServiceName, healthChecks }) => {
    return (
      <div className="ds-l-col">
        Health Checks:
        {Object.keys(healthChecks).map((key, i) => {
          const healthCheckObj: DateTimeHealthStatusValueTuple = healthChecks[key];
          if (!healthCheckObj) {
            return null;
          }
          if ((healthCheckObj.length !== 2) ||
            !((typeof healthCheckObj[0] === 'string') && (healthCheckObj[1] in HealthStatus))) {
            console.error(`Unexpected service health status: ${healthCheckObj}`);
            return null;
          }
          return (
            <div key={i}>
              <Accordion bordered>
                <HealthCheckListItem environmentName={environmentName}
                                     tenantName={tenantName}
                                     rootServiceName={rootServiceName}
                                     healthCheckName={key}
                                     healthCheckStatus={healthChecks[key][1]}/>
              </Accordion>
            </div>
          );
        })}
      </div>
    )
  }

export default HealthCheckList;
