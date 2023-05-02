import React, { useEffect, useState } from 'react';

import { createSonarClient } from 'helpers/ApiHelper';
import { DateTimeDoubleValueTuple, HealthCheckModel, HealthCheckType,
  HealthStatus, ServiceConfiguration, ServiceHierarchyConfiguration } from 'api/data-contracts';
import { IHealthCheckDefinition } from 'types';
import { AccordionItem } from '@cmsgov/design-system';
import { chartsFlexContainer, chartsFlexTable, chartsFlexThreshold } from './HealthCheckListItem.Style';
import HealthCheckListItemTimeSeriesChart from './HealthCheckListItemTimeSeriesChart'
import HealthCheckListItemTable from './HealthCheckListItemTable'
import HealthCheckListItemThresholds from './HealthCheckListItemThresholds';

const HealthCheckListItem: React.FC<{
  environmentName: string,
  tenantName: string,
  rootServiceName?: string | null,
  healthCheckName: string,
  healthCheckStatus: string
}> = ({ environmentName, tenantName, rootServiceName, healthCheckName, healthCheckStatus}) => {
  const [svcHierarchyCfg, setSvcHierarchyCfg] = useState<ServiceHierarchyConfiguration | null>(null);
  const [tsData, setTsData] = useState<DateTimeDoubleValueTuple[] | null>(null);


  useEffect(() => {
    const sonarClient = createSonarClient();
    sonarClient.getTenant(environmentName, tenantName)
      .then((res) => {
        setSvcHierarchyCfg(res.data);
      })
      .catch(e => console.log(`Error Service Hierarchy Configuration: ${e.message}`));

    if (rootServiceName != null){
      sonarClient.getHealthCheckData(environmentName, tenantName, rootServiceName, healthCheckName)
        .then((res) => {
          setTsData(res.data);
        })
        .catch(e => console.log(`Error fetching Time Series Data: ${e.message}`));
    }

  }, [environmentName, tenantName, rootServiceName, healthCheckName]);

  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;



  //Transform Date to Timestamps, ts data can be in one of two forms https://apexcharts.com/docs/series/
  const transformedData: number[][] | undefined = tsData?.map(data =>
    [new Date(data[TIMESTAMP_DATA]).getTime(), Number(data[HEALTHSTATUS_DATA])]
  );

  return (
    <AccordionItem heading={`${healthCheckName}: ${healthCheckStatus}`}>
      {
        svcHierarchyCfg?.services?.map((s: ServiceConfiguration) => s.healthChecks
          ?.filter((hc: HealthCheckModel) => hc.name === healthCheckName && hc.type !== HealthCheckType.HttpRequest)
          .map((hc: HealthCheckModel) =>
            { transformedData != null &&
              <HealthCheckListItemTimeSeriesChart svcDefinitions={hc.definition as IHealthCheckDefinition}
                                                  healthCheckName={healthCheckName}
                                                  timeSeriesData={transformedData}
              />
            }
          )
        )
      }

      <div style={chartsFlexContainer}>
        {  tsData?.map(data =>
          console.log(healthCheckName, new Date(data[TIMESTAMP_DATA]).getTime(), Number(data[HEALTHSTATUS_DATA]))
        )


        }
        <div style={chartsFlexTable}>
          { transformedData != null &&
            <HealthCheckListItemTable timeSeriesData={transformedData}/>
          }
        </div>

        <div style={chartsFlexThreshold}>

          <HealthCheckListItemThresholds svcHierarchyCfg={svcHierarchyCfg}
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

