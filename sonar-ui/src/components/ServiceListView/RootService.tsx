import React from 'react';

import { ServiceHierarchyHealth } from 'api/data-contracts';
import { getHealthStatusIndicator } from 'helpers/ServiceHierarchyHelper';
import { HeadingContainer, RootServiceContainer } from 'styles';
import ChildService from './ChildService';
import HealthCheckList from './HealthCheckList';

const RootService: React.FC<{
  rootService: ServiceHierarchyHealth,
  services: ServiceHierarchyHealth[]
}> =
  ({ rootService, services }) => {
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
              <HealthCheckList healthChecks={rootService.healthChecks}/>
            </div>
          ) : null}
          {rootService.children && rootService.children.length > 0 ?
            <>
              <div style={HeadingContainer}>
                Services:
              </div>
              <ul>
                {rootService.children.map(child => (
                  <li key={child.name}>
                    <ChildService childService={child} services={services}/>
                  </li>
                ))}
              </ul>
            </>
            : null}
        </div>
      </div>
    )
  }

export default RootService;
