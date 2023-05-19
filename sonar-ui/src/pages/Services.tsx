import React, { useEffect, useState } from 'react';
import {
  DateTimeHealthStatusValueTuple,
  ProblemDetails, ServiceConfiguration,
  ServiceHierarchyConfiguration,
  ServiceHierarchyHealth
} from 'api/data-contracts';
import RootService from 'components/Services/RootService';
import { createSonarClient } from 'helpers/ApiHelper';
import { HttpResponse } from 'api/http-client';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import { useQuery } from 'react-query';
import { useSearchParams } from 'react-router-dom';
import StatusHistoryDrawer from '../components/Services/StatusHistory/StatusHistoryDrawer';

const Services = () => {
  const sonarClient = createSonarClient();

  const [searchParams, setSearchParams] = useSearchParams();

  // TODO(BATAPI-253): eliminate this test values once the Environments list
  //  properly links to the Services view
  const environmentName = searchParams.get('environment') ?? 'foo';
  const tenantName = searchParams.get('tenant') ?? 'baz';

  const [showDrawer, setShowDrawer] = useState(false);
  const [selectedTileId, setSelectedTileId] = useState<string>('');
  const [statusHistoryViewData, setStatusHistoryViewData] = useState<StatusHistoryView | null>(null);

  const hierarchyHealthQuery = useQuery<ServiceHierarchyHealth[], Error>(
    ['services'],
    () => sonarClient.getServiceHierarchyHealth(environmentName, tenantName).then((res) => res.data)
  );

  const hierarchyConfigQuery = useQuery<ServiceHierarchyConfiguration, Error>({
      queryKey: ['ServiceHierarchyConfig'],
      queryFn: () => sonarClient.getTenant(environmentName, tenantName).then((res) => res.data)
    }
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
    setSelectedTileId('');
  }

  if (hierarchyConfigQuery.data && hierarchyHealthQuery.data) {
    const serviceConfigLookup =
      hierarchyConfigQuery.data.services.reduce(
        (lookup, svc) => { lookup[svc.name] = svc; return lookup },
        {} as { [key: string]: ServiceConfiguration }
      );

    return (
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
          {hierarchyConfigQuery.data.rootServices.map(rootService => (
            <div key={rootService}>
              <RootService
                environmentName={environmentName}
                tenantName={tenantName}
                service={serviceConfigLookup[rootService]}
                serviceHealth={hierarchyHealthQuery.data.filter(svc => svc.name === rootService)[0]}
                serviceConfigurationLookup={serviceConfigLookup}
                addTimestamp={addTimestamp}
                closeDrawer={closeDrawer}
                selectedTileId={selectedTileId}
              />
            </div>))}
        </div>
      </section>
    );
  } else if (hierarchyHealthQuery.error || hierarchyConfigQuery.error) {
    return (
      <section className="ds-l-container">
        {hierarchyConfigQuery.error && <p>Error fetching service hierarchy configuration: {hierarchyConfigQuery.error.message}</p>}
        {hierarchyHealthQuery.error && <p>Error fetching service hierarchy health: {hierarchyHealthQuery.error.message}</p>}
      </section>
    );
  } else {
    return (<section className="ds-l-container">Loading ...</section>);
  }
}

export default Services;
