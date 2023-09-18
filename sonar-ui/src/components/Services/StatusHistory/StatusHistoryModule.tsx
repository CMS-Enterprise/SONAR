import { Spinner } from '@cmsgov/design-system';
import React, { useContext, useEffect, useState } from 'react';
import {  DateTimeHealthStatusValueTuple,  ServiceHierarchyHealth,} from 'api/data-contracts';
import { ServiceOverviewHeaderStyle } from '../ServiceOverview.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { useGetServiceHealthHistory } from '../Services.Hooks';
import StatusHistoryTile from './StatusHistoryTile';
import { StatusHistoryTileContainerStyle } from './StatusHistory.Style';

const StatusHistoryModule: React.FC<{
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  servicePath: string,
  serviceHealth: ServiceHierarchyHealth
}> =
  ({
    addTimestamp,
    closeDrawer,
    selectedTileId,
    servicePath,
    serviceHealth,
  }) => {
    const context = useContext(ServiceOverviewContext)!;
    const [diffDates, setDiffDates] = useState(false);
    const { isLoading, data } =
      useGetServiceHealthHistory(
        context.environmentName,
        context.tenantName,
        servicePath);

    useEffect(() => {
      let currDate = '';
      if (data?.aggregateStatus) {
        for (let i = 0; i < data?.aggregateStatus?.length; i++) {
          const currItem = data?.aggregateStatus[i];
          const localDateString = new Date(currItem[0]).toDateString();
          if (currDate !== '') {
            if (currDate !== localDateString) {
              setDiffDates(true);
              break;
            }
          }
          currDate = localDateString;
        }
      }
    }, [data])

    return (
      <>
        <div css={ServiceOverviewHeaderStyle}>
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
                showDate={diffDates}
              />
            ))}
          </div>
        )}
      </>
    );
  };

export default StatusHistoryModule;
