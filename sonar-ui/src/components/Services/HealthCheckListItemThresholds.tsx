import React from 'react';

import {
  HealthCheckModel,
  HealthCheckType,
  ServiceConfiguration,
} from 'api/data-contracts';
import { getOperatorPunctuation } from 'helpers/ServiceHierarchyHelper';
import { IHealthCheckCondition, IHealthCheckDefinition, IHealthCheckHttpCondition } from 'types';

const TimeSeriesThresholds: React.FC<{
  service: ServiceConfiguration,
  healthCheck: HealthCheckModel,
  healthCheckStatus: string
}> = ({ service, healthCheck, healthCheckStatus }) => {

  const displayThreshold = (metricType?: HealthCheckType, definition?: IHealthCheckDefinition) => {
    switch (metricType) {
      case HealthCheckType.HttpRequest:
        return (
          <div>
            <b>Http Request</b>
            {definition?.conditions.map((c: IHealthCheckHttpCondition, index: number) =>
              <div key={healthCheck.name + '-httpCondition-' + index}>
                {c.type === 'HttpStatusCode' &&
                  <span><b>{c.status}</b>: {c.type} in [{(index ? ', ' : '') + c.statusCodes}] </span>
                }
                {c.type === 'HttpResponseTime' &&
                  <span><b>{c.status}</b>: {c.type} {'>'} {c.responseTime} </span>
                }
              </div>
            )}
          </div>
        )
      case HealthCheckType.LokiMetric:
      case HealthCheckType.PrometheusMetric:
        return (
          <div>
            <b>{(metricType === HealthCheckType.LokiMetric) ? 'Loki Query' : ' Prometheus Query'}</b>: {service.name}/{healthCheck.name}<br />
            <b>Expression</b>: {definition?.expression} <br /><br />
            {definition?.conditions.map((c: IHealthCheckCondition, index: number) => (
              <div key={healthCheck.name + '-queryCondition-' + index}>
                <span><b>{c.status}</b>: Value {getOperatorPunctuation(c.operator)} {c.threshold} </span>
              </div>)
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
      {
        <div key={healthCheck.name + '-tsThresholds'}>
          {displayThreshold(healthCheck.type, (healthCheck.definition as IHealthCheckDefinition))}
        </div>
      }
    </div>
  )
}

export default TimeSeriesThresholds;

