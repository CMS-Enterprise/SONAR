import { Spinner } from '@cmsgov/design-system';
import React from 'react';
import { useQuery } from 'react-query';
import {
  DateTimeHealthStatusValueTuple,
  ProblemDetails,
  ServiceHierarchyHealth,
  ServiceHierarchyHealthHistory
} from 'api/data-contracts';
import { HttpResponse } from 'api/http-client';
import { createSonarClient } from 'helpers/ApiHelper';
import { calculateHistoryRange } from 'helpers/StatusHistoryHelper';
import { getContainerStyle } from '../RootService.Style';
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
  const sonarClient = createSonarClient();

  const { isLoading, isError, data, error } = useQuery<ServiceHierarchyHealthHistory, Error>(
    ["statusHistory", environmentName, tenantName, rootServiceName],
    () => sonarClient.getServiceHealthHistory(environmentName, tenantName, rootServiceName, calculateHistoryRange())
      .then((res: HttpResponse<ServiceHierarchyHealthHistory, ProblemDetails | void>) => {
        console.log(res.data);
        return res.data;
      })
  );

  return (
    <>
      <div css={getContainerStyle()} >
        Status History:
      </div>
      {isLoading ? (<Spinner />) : (
        <div css={getContainerStyle()} >
          {data?.aggregateStatus?.map((item, index) => (
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
