import { useTheme } from '@emotion/react';
import React from 'react';
import { Accordion } from '@cmsgov/design-system';

import {
  DateTimeHealthStatusValueTuple, ServiceConfiguration,
  ServiceHierarchyHealth,
} from 'api/data-contracts';
import StatusHistoryModule from './StatusHistory/StatusHistoryModule';
import ChildService from './ChildService';
import HealthCheckList from './HealthCheckList';
import {
  getContainerStyle,
  getRootServiceStyle
} from './RootService.Style';

const RootService: React.FC<{
  environmentName: string,
  tenantName: string,
  service: ServiceConfiguration,
  serviceHealth: ServiceHierarchyHealth,
  serviceConfigurationLookup: { [key: string]: ServiceConfiguration },
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string
}> =
  ({
    environmentName,
    tenantName,
    service,
    serviceHealth,
    serviceConfigurationLookup,
    addTimestamp,
    closeDrawer,
    selectedTileId
  }) => {
    const theme = useTheme();

    return (
      <div css={getRootServiceStyle(theme, serviceHealth.aggregateStatus)}>
        <div css={getContainerStyle()}>
          {serviceHealth.name}
        </div>
        <div>
          <StatusHistoryModule
            addTimestamp={addTimestamp}
            closeDrawer={closeDrawer}
            selectedTileId={selectedTileId}
            servicePath={[serviceHealth.name]}
            serviceHealth={serviceHealth}
            environmentName={environmentName}
            tenantName={tenantName}
          />
        </div>
        <div>
          {service.healthChecks ? (
            <div>
              <HealthCheckList
                environmentName={environmentName}
                tenantName={tenantName}
                service={service}
                healthCheckStatuses={serviceHealth.healthChecks} />
            </div>
          ) : null}
          {service.children && service.children.length > 0 ?
            <>
              <div className="ds-l-col">
                Services:
              </div>
              <Accordion bordered>
                {service.children.map(child => (
                  <div key={child}>
                    <ChildService
                      environmentName={environmentName}
                      tenantName={tenantName}
                      servicePath={[service.name, child]}
                      service={serviceConfigurationLookup[child]}
                      serviceHealth={serviceHealth?.children && serviceHealth.children.filter(svc => svc.name === child)[0]}
                      serviceConfigurationLookup={serviceConfigurationLookup} />
                  </div>
                ))}
              </Accordion>
            </>
            : null}
        </div>
      </div>
    )
  }

export default RootService;
