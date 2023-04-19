import React from 'react';
import { Accordion } from '@cmsgov/design-system';

import { ServiceHierarchyHealth } from 'api/data-contracts';
import { getHealthStatusIndicator } from 'helpers/ServiceHierarchyHelper';
import { HeadingContainer, RootServiceContainer } from 'styles';
import ChildService from './ChildService';
import HealthCheckList from './HealthCheckList';

const RootService: React.FC<{
  environmentName: string,
  tenantName: string,
  rootService: ServiceHierarchyHealth,
  services: ServiceHierarchyHealth[]
}> =
  ({ environmentName, tenantName, rootService, services }) => {
    return (
      <div style={{
        ...RootServiceContainer,
        borderColor: getHealthStatusIndicator(rootService.aggregateStatus)
      }}
      >
        <div style={HeadingContainer}>
          {rootService.name}
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
