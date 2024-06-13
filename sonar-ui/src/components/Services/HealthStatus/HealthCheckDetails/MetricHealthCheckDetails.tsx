import React, { useContext } from 'react';
import { HealthCheckType } from '../../../../api/data-contracts';
import { IHealthCheckDefinition, IHealthCheckHttpCondition } from '../../../../types';
import { ToolTipText } from '../../../../utils/constants';
import ThemedInlineTooltip from '../../../Common/ThemedInlineTooltip';
import { getDrawerSectionHeaderStyle } from '../../Drawer.Style';
import { ServiceOverviewContext } from '../../ServiceOverviewContext';
import { useGetServiceHealthCheckData } from '../../Services.Hooks';
import MetricHealthCheckThresholds from './MetricHealthCheck/MetricHealthCheckThresholds';
import MetricHealthCheckDataTable from './MetricHealthCheck/MetricHealthCheckDataTable';
import MetricHealthCheckDataTimeSeriesChart from './MetricHealthCheck/MetricHealthCheckDataTimeSeriesChart';

const MetricHealthCheckDetails: React.FC = () => {
  const context = useContext(ServiceOverviewContext)!;
  const serviceConfiguration = context.serviceConfiguration;
  const healthCheck = context.selectedHealthCheck!;
  const healthCheckDefinition = healthCheck.definition as IHealthCheckDefinition;
  const responseTimeData = healthCheckDefinition.conditions ?
    (healthCheckDefinition.conditions as IHealthCheckHttpCondition[])
      .find((c: IHealthCheckHttpCondition) => c.type === "HttpResponseTime") :
    undefined;
  const isMetricHealthCheck = [HealthCheckType.LokiMetric, HealthCheckType.PrometheusMetric, HealthCheckType.HttpRequest].includes(healthCheck.type);

  let toolTipText;
  switch (healthCheck.type) {
    case HealthCheckType.HttpRequest:
      toolTipText = ToolTipText.statusHistory.httpCheckConditionsTip;
      break;
    case HealthCheckType.LokiMetric:
      toolTipText = ToolTipText.statusHistory.lokiConditionsTip;
      break;
    case HealthCheckType.PrometheusMetric:
      toolTipText = ToolTipText.statusHistory.prometheusConditionsTip;
      break;
    default:
      toolTipText = null;
  }

  const healthCheckData = useGetServiceHealthCheckData(
    healthCheck,
    context.environmentName,
    context.tenantName,
    serviceConfiguration.name,
    healthCheck.name);

  const timeSeriesData = (!healthCheckData || !healthCheckData.data) ? [] :
    healthCheckData.data.timeSeries.slice().reverse() as number[][]

  return (
    <>
      <h4 css={getDrawerSectionHeaderStyle}>
        Health Conditions&nbsp;-&nbsp;{healthCheck.type}
        {toolTipText ? (
          <ThemedInlineTooltip
            title={toolTipText}
          />
        ) : null}
      </h4>
      <MetricHealthCheckThresholds
        service={serviceConfiguration}
        healthCheck={healthCheck}
      />

      { isMetricHealthCheck || responseTimeData ? (
        <>
          <h4 css={getDrawerSectionHeaderStyle}>{responseTimeData ? "Http Response Time Data" : "Health Check Metrics"}</h4>
          {(timeSeriesData[0] != null) ?
            <>
              <MetricHealthCheckDataTimeSeriesChart
                key={`${healthCheck.name}-ts`}
                svcDefinitions={healthCheckDefinition}
                healthCheckName={healthCheck.name}
                timeSeriesData={timeSeriesData}
                responseTimeData={responseTimeData}
              />
              <MetricHealthCheckDataTable
                key={`${healthCheck.name}-dt`}
                healthCheckName={healthCheck.name}
                timeSeriesData={timeSeriesData}
                isResponseTimeCondition={responseTimeData ? true : false}
              />
            </>
            : <p>No data available</p>
          }
        </>
      ) : null}
    </>
  );
}

export default MetricHealthCheckDetails;
