import React from 'react';
import { DateTimeHealthStatusValueTuple, HealthStatus } from '../api/data-contracts';

export function validateHealthCheckObj(healthStatusTuple: DateTimeHealthStatusValueTuple, displayComponent: React.ReactElement) {
  if (!healthStatusTuple) {
    return null;
  }
  if ((healthStatusTuple.length !== 2) ||
    !((typeof healthStatusTuple[0] === 'string') && (healthStatusTuple[1] in HealthStatus))) {
    console.error(`Unexpected service health status: ${healthStatusTuple}`);
    return null;
  }
  return displayComponent;
}
