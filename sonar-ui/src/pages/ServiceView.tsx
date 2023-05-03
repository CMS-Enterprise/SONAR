import React, { useEffect, useState } from 'react';
import { DateTimeHealthStatusValueTuple, ProblemDetails, ServiceHierarchyHealth } from 'api/data-contracts';
import RootService from 'components/ServiceListView/RootService';
import { createSonarClient } from 'helpers/ApiHelper';
import { HttpResponse } from 'api/http-client';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import { useQuery } from 'react-query';
import StatusHistoryDrawer from '../components/StatusHistory/StatusHistoryDrawer';

const ServiceView = () => {
  const sonarClient = createSonarClient();
  const environmentName = 'foo';
  const tenantName = 'baz'

  const [showDrawer, setShowDrawer] = useState(false);
  const [selectedTileId, setSelectedTileId] = useState<string>("");
  const [statusHistoryViewData, setStatusHistoryViewData] = useState<StatusHistoryView | null>(null);

  const { isLoading, isError, data, error } = useQuery<ServiceHierarchyHealth[], Error>(
    ["services"],
    () => sonarClient.getServiceHierarchyHealth(environmentName, tenantName)
      .then((res: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        return res.data;
      })
  );

  useEffect(() => {
    if (statusHistoryViewData) {
      setShowDrawer(true);
    } else {
      setShowDrawer(false);
    }
  }, [statusHistoryViewData]);

  const addTimestamp = (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => {
    console.log(tupleData[1]);
    setSelectedTileId(tileId);
    const viewData: StatusHistoryView = {
      serviceData: serviceData,
      statusTimestampTuple: tupleData
    }
    setStatusHistoryViewData(viewData);
  }

  const closeDrawer = () => {
    setShowDrawer(false);
    setStatusHistoryViewData(null);
    setSelectedTileId("");
  }

  return data ? (
    <section className="ds-l-container">
      {showDrawer && (
        <StatusHistoryDrawer
          statusHistoryViewData={statusHistoryViewData}
          closeDrawer={closeDrawer}
          environment={environmentName}
          tenant={tenantName}
        />
      )}
      <div>
        {data.map(rootService => (
          <div key={rootService.name}>
            <RootService
              environmentName={environmentName}
              tenantName={tenantName}
              rootService={rootService}
              services={data}
              addTimestamp={addTimestamp}
              closeDrawer={closeDrawer}
              selectedTileId={selectedTileId}
            />
          </div>))}
      </div>
    </section>
  ) : null
}

export default ServiceView;
