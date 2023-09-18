import {
  DateTimeHealthStatusValueTuple,
  HealthCheckModel,
  ServiceConfiguration,
  ServiceHierarchyConfiguration,
  ServiceHierarchyHealth, ServiceVersionDetails
} from 'api/data-contracts';
import ServiceOverview from 'components/Services/ServiceOverview';
import { StatusHistoryView } from 'interfaces/global_interfaces';
import { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import { useParams } from 'react-router-dom';
import StatusHistoryDrawer from '../components/Services/StatusHistory/StatusHistoryDrawer';
import { ServiceOverviewContext } from 'components/Services/ServiceOverviewContext';
import HealthStatusDrawer from 'components/Services/HealthStatus/HealthStatusDrawer';
import { useSonarApi } from 'components/AppContext/AppContextProvider';
import { useGetServiceVersion } from '../components/Services/Services.Hooks';

const Service = () => {
  const sonarClient = useSonarApi();
  const params = useParams();
  const environmentName = params.environment as string;
  const tenantName = params.tenant as string;

  const servicePath = params['*'] || '';
  const serviceList = servicePath.split('/');
  const currentServiceIsRoot = (serviceList.length === 1) ? true : false;
  const serviceName : string = currentServiceIsRoot ?
    servicePath.split('/')[0] : servicePath.split('/').pop()!;

  const [showDrawer, setShowDrawer] = useState(false);
  const [selectedTileId, setSelectedTileId] = useState<string>('');
  const [statusHistoryViewData, setStatusHistoryViewData] = useState<StatusHistoryView | null >(null);
  const [utcTimestampDate, setUtcTimestampDate] = useState<string>('');
  const [dataHasDifferentDates, setDataHasDifferentDates] = useState(false);
  const [firstTimestampDate, setFirstTimestampDate] = useState<string>('');

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
  }, [firstTimestampDate, utcTimestampDate]);

  const addTimestamp = (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => {
    setSelectedTileId(tileId);
    if (firstTimestampDate === '') {
      setFirstTimestampDate(tupleData[0].split('T')[0]);
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
    ['services', environmentName, tenantName],
    () => sonarClient.getServiceHierarchyHealth(environmentName, tenantName).then((res) => res.data)
  );

  const hierarchyConfigQuery = useQuery<ServiceHierarchyConfiguration, Error>(
    ['ServiceHierarchyConfig', environmentName, tenantName],
    () => sonarClient.getTenant(environmentName, tenantName).then((res) => res.data)
  );

  const serviceVersionDetails = useGetServiceVersion(environmentName, tenantName, serviceName);

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


    return <ServiceOverviewContext.Provider value={{
      environmentName: environmentName,
      tenantName: tenantName,
      serviceConfiguration: serviceConfigLookup[serviceName],
      serviceHierarchyConfiguration: hierarchyConfigQuery.data,
      serviceHierarchyHealth: currentServiceHealth,
      serviceVersionDetails: serviceVersionDetails.data as ServiceVersionDetails[],
      selectedHealthCheck,
      setSelectedHealthCheck
    }}>
      <section className="ds-l-container">
        {showDrawer && (
          <StatusHistoryDrawer
            statusHistoryViewData={statusHistoryViewData}
            closeDrawer={closeDrawer}
            showDate={dataHasDifferentDates}
          />
        )}

        { selectedHealthCheck && (
          <HealthStatusDrawer onCloseClick={() => setSelectedHealthCheck(null)} />
        )}

        <div>
          <ServiceOverview
            serviceHealth={currentServiceHealth}
            servicePath={servicePath}
            addTimestamp={addTimestamp}
            closeDrawer={closeDrawer}
            selectedTileId={selectedTileId}
          />
        </div>
      </section>
    </ServiceOverviewContext.Provider>;
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
