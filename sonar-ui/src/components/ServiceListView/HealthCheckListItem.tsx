import React from 'react';

import { AccordionItem } from '@cmsgov/design-system';
import TimeSeriesChart from 'components/Charts/TimeSeriesChart'
import ChartsTable from 'components/Charts/ChartsTable'
import { chartsFlexContainer, chartsFlexItem } from 'components/Charts/charts.style';

const HealthCheckListItem: React.FC<{
  environmentName: string,
  tenantName: string,
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({ environmentName, tenantName, rootServiceName, healthCheckName, healthCheckStatus}) => {
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
        </div>
      </div>
    </AccordionItem>
  );
};

export default HealthCheckListItem;
