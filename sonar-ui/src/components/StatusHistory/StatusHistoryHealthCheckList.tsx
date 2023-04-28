import React from 'react';
import { DateTimeHealthStatusValueTuple, HealthStatus } from '../../api/data-contracts';

const StatusHistoryHealthCheckList: React.FC<{
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> = ({ healthChecks }) => {
  return (
    <div style={{marginTop: 10}}>
      <b>Health Checks:</b>
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
            {key}: {healthChecks[key][1]}
          </div>
        );
      })}
    </div>
  );
}

export default StatusHistoryHealthCheckList;
