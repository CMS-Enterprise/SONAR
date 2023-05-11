import { useTheme } from '@emotion/react';
import React from 'react';
import { Accordion } from '@cmsgov/design-system';

import {
  DateTimeHealthStatusValueTuple,
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
  rootService: ServiceHierarchyHealth,
  services: ServiceHierarchyHealth[],
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string
}> =
  ({ environmentName, tenantName, rootService, services, addTimestamp, closeDrawer, selectedTileId }) => {
    const theme = useTheme();

    return (
      <div css={getRootServiceStyle(theme, rootService.aggregateStatus)}
      >
        <div css={getContainerStyle()}>
          {rootService.name}
        </div>
        <div>
          <StatusHistoryModule
            addTimestamp={addTimestamp}
            closeDrawer={closeDrawer}
            selectedTileId={selectedTileId}
            rootService={rootService}
            environmentName={environmentName}
            tenantName={tenantName}
          />
        </div>
        <div>
          {rootService.healthChecks ? (
            <div>
              <HealthCheckList environmentName={environmentName}
                               tenantName={tenantName}
                               rootServiceName={rootService.name}
                               healthChecks={rootService.healthChecks}/>
            </div>
          ) : null}
          {rootService.children && rootService.children.length > 0 ?
            <>
              <div className="ds-l-col" >
                Services:
              </div>
              <Accordion bordered>
                {rootService.children.map(child => (
                  <div key={child.name}>
                    <ChildService environmentName={environmentName}
                                  tenantName={tenantName}
                                  servicePath={rootService.name}
                                  childService={child}
                                  services={services}/>
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
