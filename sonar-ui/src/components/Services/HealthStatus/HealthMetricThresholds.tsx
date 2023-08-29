import React from 'react';
import {
  HealthCheckModel,
  HealthCheckType,
  ServiceConfiguration,
} from 'api/data-contracts';
import { getOperatorPunctuation } from 'helpers/ServiceHierarchyHelper';
import { IHealthCheckCondition, IHealthCheckDefinition, IHealthCheckHttpCondition } from 'types';
import { DynamicTextFontStyle } from 'App.Style'
import ExternalLinkIcon from 'components/Icons/ExternalLinkIcon';

const HealthMetricThresholds: React.FC<{
  service: ServiceConfiguration,
  healthCheck: HealthCheckModel,
  healthCheckStatus: string
}> = ({ service, healthCheck, healthCheckStatus }) => {

  const displayThreshold = (metricType?: HealthCheckType, definition?: IHealthCheckDefinition) => {
    switch (metricType) {
      case HealthCheckType.HttpRequest:
        return (
          <div>
            {definition?.url && (
              <p>
                <b>Uri: </b>
                <a target='_blank' rel="noreferrer" href={definition.url}>
                  {definition.url}&nbsp;
                  <ExternalLinkIcon className='ds-u-font-size--sm ds-u-valign--top'/>
                </a>
              </p>
            )}

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
            <b>{(metricType === HealthCheckType.LokiMetric) ? 'Loki Query' : ' Prometheus Query'}</b>: <span css={DynamicTextFontStyle}>{service.name}/{healthCheck.name}</span><br />
            <b>Expression</b>: <span css={DynamicTextFontStyle}>{definition?.expression}</span> <br /><br />
            {definition?.conditions.map((c: IHealthCheckCondition, index: number) => (
              <div key={healthCheck.name + '-queryCondition-' + index}>
                <span><b>{c.status}</b>: <span css={DynamicTextFontStyle}>Value {getOperatorPunctuation(c.operator)} {c.threshold}</span> </span>
              </div>)
            )}
          </div>
        )
      case HealthCheckType.Internal:
        return (
          <div>
            <p><b>Uri</b>: <span css={DynamicTextFontStyle}>{service.url}</span></p>
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

export default HealthMetricThresholds;
