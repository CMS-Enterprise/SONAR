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
import { StaticTextFontStyle } from '../../../App.Style';
import { ServiceOverviewHeaderStyle } from '../ServiceOverview.Style';
import StatusHistoryTile from './StatusHistoryTile';
import { StatusHistoryTileContainerStyle } from './StatusHistory.Style';

const StatusHistoryModule: React.FC<{
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  servicePath: string[],
  serviceHealth: ServiceHierarchyHealth,
  environmentName: string,
  tenantName: string
}> =
  ({
    addTimestamp,
    closeDrawer,
    selectedTileId,
    servicePath,
    serviceHealth,
    environmentName,
    tenantName
  }) => {
    const sonarClient = createSonarClient();

    const { isLoading, isError, data, error } = useQuery<ServiceHierarchyHealthHistory, Error>(
      ['statusHistory', environmentName, tenantName, servicePath.join('/')],
      () => sonarClient.getServiceHealthHistory(environmentName, tenantName, servicePath.join('/'), calculateHistoryRange())
        .then((res: HttpResponse<ServiceHierarchyHealthHistory, ProblemDetails | void>) => {
          return res.data;
        })
    );

    return (
      <>
        <div css={[ServiceOverviewHeaderStyle, StaticTextFontStyle]}>
          Status History
        </div>
        {isLoading ? (<Spinner />) : (
          <div css={StatusHistoryTileContainerStyle}>
            {data?.aggregateStatus?.map((item, index) => (
              <StatusHistoryTile
                key={`${serviceHealth.name}-${index}`}
                id={`${serviceHealth.name}-${index}`}
                statusTimestampTuple={item}
                addTimestamp={addTimestamp}
                closeDrawer={closeDrawer}
                selectedTileId={selectedTileId}
                serviceHealth={serviceHealth}
              />
            ))}
          </div>
        )}
      </>
    );
  };

export default StatusHistoryModule;
