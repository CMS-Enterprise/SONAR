import React, { useEffect, useState } from 'react';
import { DateTimeHealthStatusValueTuple, ProblemDetails, ServiceHierarchyHealth } from 'api/data-contracts';
import RootService from 'components/ServiceListView/RootService';
import { createSonarClient } from 'helpers/ApiHelper';
import { HttpResponse } from 'api/http-client';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import StatusHistoryDrawer from '../components/StatusHistory/StatusHistoryDrawer';

const ServiceView = () => {
  const [services, setServices] = useState<ServiceHierarchyHealth[] | null>(null);
  const environmentName = 'foo';
  const tenantName = 'baz'
  const [showDrawer, setShowDrawer] = useState(false);
  const [selectedTileId, setSelectedTileId] = useState<string>("");
  const [statusHistoryViewData, setStatusHistoryViewData] = useState<StatusHistoryView | null>(null);

  useEffect(() => {
    // create sonar client
    const sonarClient = createSonarClient();

    sonarClient.getServiceHierarchyHealth(environmentName, tenantName)
      .then((res: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        setServices(res.data);
      })
      .catch((e: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        console.log(`Error fetching health metrics: ${e.error}`);
      });
  }, []);

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

  return services ? (
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
        {services.map(rootService => (
          <div key={rootService.name}>
            <RootService
              environmentName={environmentName}
              tenantName={tenantName}
              rootService={rootService}
              services={services}
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
