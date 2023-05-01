import React from 'react';
import { DateTimeHealthStatusValueTuple } from '../../api/data-contracts';
import { validateHealthCheckObj } from '../../helpers/HealthCheckHelper';

const StatusHistoryHealthCheckList: React.FC<{
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> = ({ healthChecks }) => {
  return (
    <div className={"drawer-container"}>
      <b>Health Checks:</b>
      {Object.keys(healthChecks).map((key, i) => {
        const healthCheckObj: DateTimeHealthStatusValueTuple = healthChecks[key];
        const displayComponent = (
          <div key={i}>
            {key}: {healthChecks[key][1]}
          </div>
        );

        return validateHealthCheckObj(healthCheckObj, displayComponent);
      })}
    </div>
  );
}

export default StatusHistoryHealthCheckList;
