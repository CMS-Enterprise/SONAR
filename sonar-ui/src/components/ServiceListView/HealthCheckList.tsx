import React from 'react';

import { DateTimeHealthStatusValueTuple, HealthStatus } from 'api/data-contracts';
import { HeadingContainer, HealthCheckItem } from 'styles';

const HealthCheckList: React.FC<{
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> =
  ({ healthChecks }) => {
    return (
      <div style={HeadingContainer}>
        Health Checks:
        <ul>
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
            return (<li style={HealthCheckItem} key={i}>{key}: {healthCheckObj[1]}</li>);
          })}
        </ul>
      </div>
    )
  }

export default HealthCheckList;
