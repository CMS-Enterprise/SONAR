import { Drawer } from '@cmsgov/design-system';
import React from 'react';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import StatusHistoryHealthCheckList from './StatusHistoryHealthCheckList';

const StatusHistoryDrawer: React.FC<{
  statusHistoryViewData: StatusHistoryView | null,
  closeDrawer: () => void,
  environment: string,
  tenant: string
}> = ({ statusHistoryViewData, closeDrawer, environment, tenant }) => {

  return (
    <Drawer heading={"Selected Timestamps"} onCloseClick={closeDrawer}>
      {statusHistoryViewData && (
        <>
          <div>
            <b>{statusHistoryViewData.serviceData.name}</b>
          </div>
          <div>
            {statusHistoryViewData.statusTimestampTuple[0]}: {statusHistoryViewData.statusTimestampTuple[1]}
          </div>
          {statusHistoryViewData.serviceData.healthChecks ? (
            <StatusHistoryHealthCheckList healthChecks={statusHistoryViewData.serviceData.healthChecks} />) :
            null}
        </>

      )}
    </Drawer>
  )
}

export default StatusHistoryDrawer;
