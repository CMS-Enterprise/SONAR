import React from 'react';

import { createSonarClient } from 'helpers/ApiHelper';
import { DateTimeDoubleValueTuple, HealthCheckModel, HealthCheckType,
  ServiceConfiguration, ServiceHierarchyConfiguration } from 'api/data-contracts';
import { IHealthCheckDefinition } from 'types';
import { AccordionItem } from '@cmsgov/design-system';
import {
  getChartsFlexTableStyle,
  getChartsFlexThresholdStyle
} from './HealthCheckListItem.Style';
import HealthCheckListItemTimeSeriesChart from './HealthCheckListItemTimeSeriesChart'
import HealthCheckListItemTable from './HealthCheckListItemTable'
import HealthCheckListItemThresholds from './HealthCheckListItemThresholds';
import { useQuery } from 'react-query';

const HealthCheckListItem: React.FC<{
  environmentName: string,
  tenantName: string,
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({ environmentName, tenantName, rootServiceName, healthCheckName, healthCheckStatus}) => {
  const sonarClient = createSonarClient();

  const svcHierarchyCfg = useQuery<ServiceHierarchyConfiguration, Error>({
    queryKey: ['ServiceHierarchyConfig'],
    queryFn: () => sonarClient.getTenant(environmentName, tenantName)
      .then((res) => res.data
      )
    }
  )
  const healthCheckData = useQuery<DateTimeDoubleValueTuple[], Error>({
    queryKey: ['HealthCheckData-'+healthCheckName],
    queryFn: () => sonarClient.getHealthCheckData(environmentName, tenantName, rootServiceName ? rootServiceName : "", healthCheckName)
      .then((res) => res.data
      )
    }
  )

  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  //Transform Date to Timestamps, ts data can be in one of two forms https://apexcharts.com/docs/series/
  const transformedData = healthCheckData.data?.map(data =>
    [data[TIMESTAMP_DATA], data[HEALTHSTATUS_DATA]]
  ).reverse() as number[][];

  return (
    <AccordionItem heading={`${healthCheckName}: ${healthCheckStatus}`}>
      {(svcHierarchyCfg.data != null) && (transformedData != null) && (transformedData.length !== 0) &&
        svcHierarchyCfg.data.services?.map((s: ServiceConfiguration) =>
          s.healthChecks
            ?.filter((hc: HealthCheckModel) => hc.name === healthCheckName && hc.type !== HealthCheckType.HttpRequest)
            .map((hc: HealthCheckModel) =>
              (
                <div key={healthCheckName + '-tsData'}>
                  <HealthCheckListItemTimeSeriesChart
                    svcDefinitions={hc.definition as IHealthCheckDefinition}
                    healthCheckName={healthCheckName}
                    timeSeriesData={transformedData}
                  />

                  <div css={getChartsFlexTableStyle()}>
                    <HealthCheckListItemTable
                      healthCheckName={healthCheckName}
                      timeSeriesData={transformedData}/>
                  </div>
                </div>
              )
            )
        )
      }

      <div css={getChartsFlexThresholdStyle()}>
        {svcHierarchyCfg.data != null &&
          <HealthCheckListItemThresholds svcHierarchyCfg={svcHierarchyCfg.data}
                                         rootServiceName={rootServiceName}
                                         healthCheckName={healthCheckName}
                                         healthCheckStatus={healthCheckStatus}
          />
        }
      </div>
    </AccordionItem>
  );
};

export default HealthCheckListItem;

