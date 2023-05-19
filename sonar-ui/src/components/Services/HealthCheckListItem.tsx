import { AccordionItem } from '@cmsgov/design-system';
import { DateTimeDoubleValueTuple, HealthCheckModel, HealthCheckType, ServiceConfiguration } from 'api/data-contracts';

import { createSonarClient } from 'helpers/ApiHelper';
import React from 'react';
import { useQuery } from 'react-query';
import { IHealthCheckDefinition } from 'types';
import { getChartsFlexTableStyle, getChartsFlexThresholdStyle } from './HealthCheckListItem.Style';
import HealthCheckListItemTable from './HealthCheckListItemTable'
import HealthCheckListItemThresholds from './HealthCheckListItemThresholds';
import HealthCheckListItemTimeSeriesChart from './HealthCheckListItemTimeSeriesChart'

const HealthCheckListItem: React.FC<{
  environmentName: string,
  tenantName: string,
  service: ServiceConfiguration,
  healthCheck: HealthCheckModel,
  healthCheckStatus: string
}> = ({ environmentName, tenantName, service, healthCheck, healthCheckStatus }) => {
  const sonarClient = createSonarClient();

  const healthCheckData = useQuery<DateTimeDoubleValueTuple[], Error>({
    queryKey: ['HealthCheckData-' + healthCheck.name],
    queryFn: () => sonarClient.getHealthCheckData(environmentName, tenantName, service.name, healthCheck.name)
      .then((res) => res.data.timeSeries)
  });

  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  //Transform Date to Timestamps, ts data can be in one of two forms https://apexcharts.com/docs/series/
  const transformedData = healthCheckData.data?.map(data =>
    [data[TIMESTAMP_DATA], data[HEALTHSTATUS_DATA]]
  ).reverse() as number[][];

  return (
    <AccordionItem heading={`${healthCheck.name}: ${healthCheckStatus}`}>
      {healthCheck && healthCheck.type !== HealthCheckType.HttpRequest && transformedData?.length &&
        <div key={healthCheck.name + '-tsData'}>
          <HealthCheckListItemTimeSeriesChart
            svcDefinitions={healthCheck.definition as IHealthCheckDefinition}
            healthCheckName={healthCheck.name}
            timeSeriesData={transformedData}
          />

          <div css={getChartsFlexTableStyle()}>
            <HealthCheckListItemTable
              healthCheckName={healthCheck.name}
              timeSeriesData={transformedData} />
          </div>
        </div>
      }

      <div css={getChartsFlexThresholdStyle()}>
        {
          <HealthCheckListItemThresholds
            service={service}
            healthCheck={healthCheck}
            healthCheckStatus={healthCheckStatus}
          />
        }
      </div>
    </AccordionItem>
  );
};

export default HealthCheckListItem;

