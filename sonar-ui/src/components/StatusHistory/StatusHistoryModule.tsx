import { Spinner } from '@cmsgov/design-system';
import React, { useEffect, useState } from 'react';
import {
  DateTimeHealthStatusValueTuple,
  ProblemDetails,
  ServiceHierarchyHealth,
  ServiceHierarchyHealthHistory
} from '../../api/data-contracts';
import { HttpResponse } from '../../api/http-client';
import { createSonarClient } from '../../helpers/ApiHelper';
import { HeadingContainer, StatusHistoryContainer } from '../../styles';
import StatusHistoryTile from './StatusHistoryTile';

const StatusHistoryModule: React.FC<{
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  rootService: ServiceHierarchyHealth,
  environmentName: string,
  tenantName: string
}> = ({ addTimestamp, closeDrawer, selectedTileId, rootService, environmentName, tenantName }) => {
  // store rootService name
  const rootServiceName = rootService.name ? rootService.name : "";
  const [historyData, setHistoryData] = useState<ServiceHierarchyHealthHistory | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const sonarClient = createSonarClient();
    // get start & end dates
    const dateObj = new Date();
    const end = dateObj.toISOString();
    (dateObj.setHours(dateObj.getHours() - 12));
    const start = dateObj.toISOString();
    console.log(`start: ${start}, end: ${end}`);
    const step = 2160;
    sonarClient.getServiceHealthHistory(environmentName, tenantName, rootServiceName, {start, end, step})
      .then((res: HttpResponse<ServiceHierarchyHealthHistory, ProblemDetails | void>) => {
        console.log(res.data);
        setHistoryData(res.data);
        setLoading(false);
      })
      .catch((e: HttpResponse<ServiceHierarchyHealthHistory, ProblemDetails | void>) => {
        console.log(`Error fetching health metrics: ${e.error}`);
      });
  }, []);

  return (
    <>
      <div style={HeadingContainer}>
        Status History:
      </div>
      {loading ? (<Spinner />) : (
        <div style={StatusHistoryContainer}>
          {historyData?.aggregateStatus?.map((item, index) => (
            <StatusHistoryTile
              key={`${rootServiceName}-${index}`}
              id={`${rootServiceName}-${index}`}
              statusTimestampTuple={item}
              addTimestamp={addTimestamp}
              closeDrawer={closeDrawer}
              selectedTileId={selectedTileId}
              rootService={rootService}
            />
          ))}
        </div>
      )}
    </>

  )
}

export default StatusHistoryModule;
