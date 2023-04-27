import React, { useEffect, useState } from 'react';

import { createSonarClient } from 'helpers/ApiHelper';
import { HealthCheckModel, HealthCheckType, ServiceConfiguration, ServiceHierarchyConfiguration } from 'api/data-contracts';
import { IHealthCheckDefinition } from 'types';
import { AccordionItem } from '@cmsgov/design-system';
import { chartsFlexContainer, chartsFlexTable, chartsFlexThreshold } from './HealthCheckListItem.Style';
import HealthCheckListItemTimeSeriesChart from './HealthCheckListItemTimeSeriesChart'
import HealthCheckListItemTable from './HealthCheckListItemTable'
import HealthCheckListItemThreshold from './Thresholds';

const HealthCheckListItem: React.FC<{
  environmentName: string,
  tenantName: string,
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({ environmentName, tenantName, rootServiceName, healthCheckName, healthCheckStatus}) => {
  const [svcHierarchyCfg, setSvcHierarchyCfg] = useState<ServiceHierarchyConfiguration | null>(null);
  /*
  //TODO API Call to TS healthCheck
  const [tsData, setTsData] = useState<timeSeriesData[] | null>(null);

  useEffect(() => {
    const sonarClient = createSonarClient();
    sonarClient.getTsHealthcheck(environmentName, tenantName, rootServiceName, healthCheckName)
      .then((res) => {
        setTsData(res.data);
      })
      .catch(e => console.log(`Error fetching Time Series Data: ${e.message}`));
  }, []);
   */

  useEffect(() => {
    const sonarClient = createSonarClient();
    sonarClient.getTenant(environmentName, tenantName)
      .then((res) => {
        setSvcHierarchyCfg(res.data);
      })
      .catch(e => console.log(`Error Service Hierarchy Configuration: ${e.message}`));
  }, [environmentName, tenantName]);

  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  const mockData =
    [
      ['2023-04-13T03:45:24.836', 0],
      ['2023-04-13T03:45:29.836', 2],
      ['2023-04-13T03:45:34.836', 10],
      ['2023-04-13T03:45:39.836', 33],
      ['2023-04-13T03:45:44.836', 58],
      ['2023-04-13T03:45:49.836', 70]
    ];

  //Transform Date to Timestamps, ts data can be in one of two forms https://apexcharts.com/docs/series/
  const transformedData = mockData.map(data =>
    [new Date(data[TIMESTAMP_DATA]).getTime(), Number(data[HEALTHSTATUS_DATA])]
  );

  return (
    <AccordionItem heading={`${healthCheckName}: ${healthCheckStatus}`}>
      {
        svcHierarchyCfg?.services?.map((s: ServiceConfiguration) => s.healthChecks
          ?.filter((hc: HealthCheckModel) => hc.name === healthCheckName && hc.type !== HealthCheckType.HttpRequest)
          .map((hc: HealthCheckModel) =>
            <HealthCheckListItemTimeSeriesChart svcDefinitions={hc.definition as IHealthCheckDefinition}
                                                healthCheckName={healthCheckName}
                                                timeSeriesData={transformedData}
            />
          )
        )
      }

      <div style={chartsFlexContainer}>
        <div style={chartsFlexTable}>
          <HealthCheckListItemTable timeSeriesData={transformedData}/>
        </div>

        <div style={chartsFlexThreshold}>
          <HealthCheckListItemThreshold svcHierarchyCfg={svcHierarchyCfg}
                                        rootServiceName={rootServiceName}
                                        healthCheckName={healthCheckName}
                                        healthCheckStatus={healthCheckStatus}
          />
        </div>
      </div>

    </AccordionItem>
  );
};

export default HealthCheckListItem;

