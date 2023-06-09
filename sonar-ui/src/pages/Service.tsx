import {
  DateTimeHealthStatusValueTuple,
  HealthCheckModel,
  ServiceConfiguration,
  ServiceHierarchyConfiguration,
  ServiceHierarchyHealth
} from 'api/data-contracts';
import ServiceOverview from 'components/Services/ServiceOverview';
import { createSonarClient } from 'helpers/ApiHelper';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import { useLocation, useParams } from 'react-router-dom';
import StatusHistoryDrawer from '../components/Services/StatusHistory/StatusHistoryDrawer';
import { ServiceOverviewContext } from 'components/Services/ServiceOverviewContext';
import HealthStatusDrawer from 'components/Services/HealthStatus/HealthStatusDrawer';

const Service = () => {
  const sonarClient = createSonarClient();
  const params = useParams();
  const environmentName = params.environment as string;
  const tenantName = params.tenant as string;

  const location = useLocation();
  const servicePath = location.pathname.split('services/')[1];
  const serviceList = servicePath.split('/');
  const currentServiceIsRoot = (serviceList.length === 1) ? true : false;
  const serviceName : string = currentServiceIsRoot ?
    servicePath.split('/')[0] : servicePath.split('/').pop()!;

  const [showDrawer, setShowDrawer] = useState(false);
  const [selectedTileId, setSelectedTileId] = useState<string>('');
  const [statusHistoryViewData, setStatusHistoryViewData] = useState<StatusHistoryView | null >(null);
  const [utcTimestampDate, setUtcTimestampDate] = useState<string>('');
  const [dataHasDifferentDates, setDataHasDifferentDates] = useState(false);

  let firstTimestampDate = ''

  useEffect(() => {
    if (statusHistoryViewData) {
      setSelectedHealthCheck(null);
      setShowDrawer(true);
    } else {
      setShowDrawer(false);
    }
  }, [statusHistoryViewData]);

  useEffect(() => {
    if ((firstTimestampDate !== '') && (firstTimestampDate !== utcTimestampDate)) {
      setDataHasDifferentDates(true);
    }
  }, [utcTimestampDate]);

  const addTimestamp = (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => {
    setSelectedTileId(tileId);
    if (firstTimestampDate === '') {
      firstTimestampDate = tupleData[0].split('T')[0];
    }
    setUtcTimestampDate(tupleData[0].split('T')[0]);
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

  const [selectedHealthCheck, setSelectedHealthCheck] = useState<HealthCheckModel | null>();

  useEffect(() => { selectedHealthCheck && closeDrawer() }, [selectedHealthCheck])

  const hierarchyHealthQuery = useQuery<ServiceHierarchyHealth[], Error>(
    ['services'],
    () => sonarClient.getServiceHierarchyHealth(environmentName, tenantName).then((res) => res.data)
  );

  const hierarchyConfigQuery = useQuery<ServiceHierarchyConfiguration, Error>({
    queryKey: ['ServiceHierarchyConfig'],
    queryFn: () => sonarClient.getTenant(environmentName, tenantName).then((res) => res.data)
  });

  if (hierarchyConfigQuery.data && hierarchyHealthQuery.data) {
    const serviceConfigLookup =
      hierarchyConfigQuery.data.services.reduce(
        (lookup, svc) => { lookup[svc.name] = svc; return lookup },
        {} as { [key: string]: ServiceConfiguration }
      );

    let currentServiceHealth = hierarchyHealthQuery.data.filter(svc => svc.name === serviceList[0])[0];
    if (!currentServiceIsRoot) {
      // ignore the root service to iterate through child services
      serviceList.splice(0,1);

      // get current service's health hierarchy
      serviceList.forEach(childService => {
        currentServiceHealth = (
          currentServiceHealth?.children &&
          currentServiceHealth.children.filter(svc => svc.name === childService)[0]
        ) as ServiceHierarchyHealth;
      });
    }

    return (
      <ServiceOverviewContext.Provider value={{
        environmentName: environmentName,
        tenantName: tenantName,
        serviceConfiguration: serviceConfigLookup[serviceName],
        serviceHierarchyHealth: currentServiceHealth,
        selectedHealthCheck,
        setSelectedHealthCheck
      }}>
        <section className="ds-l-container">
          {showDrawer && (
            <StatusHistoryDrawer
              statusHistoryViewData={statusHistoryViewData}
              closeDrawer={closeDrawer}
              environment={environmentName}
              tenant={tenantName}
              showDate={dataHasDifferentDates}
            />
          )}

          { selectedHealthCheck && (
            <HealthStatusDrawer onCloseClick={() => setSelectedHealthCheck(null)} />
          )}

          <div>
            <ServiceOverview
              environmentName={environmentName}
              tenantName={tenantName}
              serviceConfig={serviceConfigLookup[serviceName]}
              serviceHealth={currentServiceHealth}
              addTimestamp={addTimestamp}
              closeDrawer={closeDrawer}
              selectedTileId={selectedTileId}
              showDate={dataHasDifferentDates}
            />
          </div>
        </section>
      </ServiceOverviewContext.Provider>

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

export default Service;
