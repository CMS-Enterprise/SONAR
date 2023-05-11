import React from 'react';
import { AccordionItem } from '@cmsgov/design-system';
import { ServiceHierarchyHealth } from 'api/data-contracts';
import { getChildServiceContainerStyle } from './ChildService.Style';
import HealthCheckList from './HealthCheckList';

const ChildService: React.FC<{
  environmentName: string,
  tenantName: string,
  servicePath?: string | null,
  childService: ServiceHierarchyHealth,
  services: ServiceHierarchyHealth[]
}> =
  ({ environmentName, tenantName, servicePath, childService, services }) => {
    return (
      <div css={getChildServiceContainerStyle()}>
        <AccordionItem heading={childService.name}>
          <div>
            {childService.healthChecks ? (
              <div>
                <HealthCheckList environmentName={environmentName}
                                 tenantName={tenantName}
                                 rootServiceName={`${servicePath}/${childService.name}`}
                                 healthChecks={childService.healthChecks}/>
              </div>
            ) : null}
            {childService.children && childService.children.length > 0 ?
              <>
                <div className="ds-l-col">
                  Services:
                </div>
                <ul>
                  {childService.children.map(child => (
                    <div key={child.name}>
                      <ChildService environmentName={environmentName}
                                    tenantName={tenantName}
                                    servicePath={`${servicePath}/${childService.name}`}
                                    childService={child}
                                    services={services}/>
                    </div>
                  ))}
                </ul>
              </> : null}
          </div>
        </AccordionItem>
      </div>
    )
  }

export default ChildService;
