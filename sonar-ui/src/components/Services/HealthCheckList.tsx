import React from 'react';
import { Accordion } from '@cmsgov/design-system';

import { DateTimeHealthStatusValueTuple } from 'api/data-contracts';
import HealthCheckListItem from 'components/Services/HealthCheckListItem';
import { validateHealthCheckObj } from 'helpers/HealthCheckHelper';

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
          const displayComponent = (
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

          return validateHealthCheckObj(healthCheckObj, displayComponent);
        })}
      </div>
    )
  }

export default HealthCheckList;
