import React from 'react';
import { DateTimeHealthStatusValueTuple } from "../../api/data-contracts";
import { HeadingContainer, HealthCheckItem } from "../../styles";

const HealthCheckList: React.FC<{
  healthChecks: Record<string, DateTimeHealthStatusValueTuple>
}> =
  ({ healthChecks }) => {

    return (
      <div style={HeadingContainer}>
        Health Checks:
        <ul>
          {Object.keys(healthChecks).map((key, i) => {
            let healthCheckObj: any = healthChecks[key];
            if (!healthCheckObj ||
              !Array.isArray(healthCheckObj) ||
              healthCheckObj.length !== 2) {
              return null;
            }
            console.log(healthCheckObj);
            return (<li style={HealthCheckItem} key={i}>{key}: {healthCheckObj[1]}</li>);
          })}
        </ul>
      </div>
    )
  }

export default HealthCheckList;
