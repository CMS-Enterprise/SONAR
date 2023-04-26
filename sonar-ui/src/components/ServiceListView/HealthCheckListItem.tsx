import React, { useEffect, useState } from 'react';

import { AccordionItem } from '@cmsgov/design-system';
import TimeSeriesChart from 'components/Charts/TimeSeriesChart'
import ChartsTable from 'components/Charts/ChartsTable'
import { chartsFlexContainer, chartsFlexItem } from 'components/Charts/charts.style';
import { createSonarClient } from 'helpers/ApiHelper';
import { HealthCheckDefinition, ServiceHierarchyConfiguration } from 'api/data-contracts';
import ThresholdTable from './ThresholdTable';

const HealthCheckListItem: React.FC<{
  environmentName: string,
  tenantName: string,
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({ environmentName, tenantName, rootServiceName, healthCheckName, healthCheckStatus}) => {
  const [svcHierachyCfg, setSvcHierachyCfg] = useState<ServiceHierarchyConfiguration | null>(null);
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
        setSvcHierachyCfg(res.data);
      })
      .catch(e => console.log(`Error fetching tenants: ${e.message}`));
  });

  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  const mockData =
    [
      ['2023-04-13T03:45:24.836', 0],
      ['2023-04-13T03:45:29.836', 1],
      ['2023-04-13T03:45:34.836', 2],
      ['2023-04-13T03:45:39.836', 3],
      ['2023-04-13T03:45:44.836', 4],
      ['2023-04-13T03:45:49.836', 5]
    ];

  //Transform Date to Timestamps, ts data can be in one of two forms https://apexcharts.com/docs/series/
  const transformedData = mockData.map(data =>
    [new Date(data[TIMESTAMP_DATA]).getTime(), Number(data[HEALTHSTATUS_DATA])]
  );

  return (
    <AccordionItem heading={`${healthCheckName}: ${healthCheckStatus}`}>
      <TimeSeriesChart healthCheckName={healthCheckName} timeSeriesData={transformedData} />
      <div style={chartsFlexContainer}>
        <div style={chartsFlexItem}>
          <ChartsTable timeSeriesData={transformedData}/>
        </div>

        <div style={chartsFlexItem}>

          <div>
            {svcHierachyCfg?.services?.map((s: any) =>
              s.healthChecks.filter((hc:any) => hc.name === healthCheckName).map((hc:any) =>
                <span>{hc.definition.conditions.map((c:any) => c.status)}</span>



              )
            )}
          </div>
          <ThresholdTable svcHierarchyCfg={svcHierachyCfg}
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

