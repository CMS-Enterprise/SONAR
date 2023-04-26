import React from 'react';

import { HealthCheckType, ServiceHierarchyConfiguration } from 'api/data-contracts';
import { getOperatorPunctuation } from 'helpers/ServiceHierarchyHelper';

const ThresholdTable: React.FC<{
  svcHierarchyCfg: ServiceHierarchyConfiguration | null,
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({svcHierarchyCfg, rootServiceName, healthCheckName, healthCheckStatus}) => {
  const displayThreshold = (metricType: HealthCheckType, definition:any) => {
    switch (metricType) {
      case HealthCheckType.HttpRequest:
        return(
          <div>
            <b>Http Request</b>
            {definition.conditions.map((c:any, index:number) =>
              <div>
                { c.type === 'HttpStatusCode' &&
                  <span><b>{c.status}</b>: {c.type} in [{(index ? ',' + ' ' : '') + c.statusCodes}] </span>
                }
                { c.type === 'HttpResponseTime' &&
                  <span><b>{c.status}</b>: {c.type} {'>'} {c.responseTime} </span>
                }
              </div>
            )}
          </div>
        )
      case HealthCheckType.LokiMetric:
      case HealthCheckType.PrometheusMetric:
        return(
          <div>
            <b>{(metricType === HealthCheckType.LokiMetric)?'Loki Query' :' Prometheus Query'}</b>: {rootServiceName}/{healthCheckName}<br />
            <b>Expression</b>: {definition.expression} <br /><br />
            {definition.conditions.map((c:any, index:number) =>
              <div>
                <span><b>{c.status}</b>: Value {getOperatorPunctuation(c.operator)} {c.threshold} </span>
              </div>
            )}
          </div>
        )
      default:
        console.error('Unexpected metric type.');
        break;
    }
  }

  return (
    <div>
      {svcHierarchyCfg?.services?.map((s: any) =>
        s.healthChecks.filter((hc:any) => hc.name === healthCheckName).map((hc:any) =>
          <div>{displayThreshold(hc.type, hc.definition)}</div>
        )
      )}
    </div>
  )
}

export default ThresholdTable;

