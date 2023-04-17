import React from 'react';

import { AccordionItem } from '@cmsgov/design-system';

const HealthCheckListItem: React.FC<{
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({ rootServiceName, healthCheckName, healthCheckStatus}) => {

  return (
    <AccordionItem heading={`${healthCheckName}: ${healthCheckStatus}`}>
      {healthCheckName.toLowerCase().includes("http") &&
        <p>
          <b>Offline</b>: Value {'>'} 50<br />
          <b>Degraded</b>: Response Time {'>'} 500ms<br />
          <b>Online</b>: StatusCode in (200, 201, 204)
        </p>
      }
      {
        healthCheckName.toLowerCase().includes("loki") &&
        <p>
          <b>Loki Query</b>: {rootServiceName}/{healthCheckName}<br /><br />
          <b>Offline</b>: Value {'>'} 4<br />
          <b>Degraded</b>: Value {'>'} 3<br />
          <b>AtRisk</b>: Value {'>'} 2
        </p>
      }
      {
        !healthCheckName.toLowerCase().includes("http") &&
        !healthCheckName.toLowerCase().includes("loki") &&
        <p>
          <b>Prometheus Query</b>: {rootServiceName}/{healthCheckName}<br /><br />
          <b>Offline</b>: Value {'>'} 60<br />
          <b>Degraded</b>: Value {'>'} 20
        </p>
      }
    </AccordionItem>
  );
};

export default HealthCheckListItem;
