import React from 'react';
import { AccordionItem } from '@cmsgov/design-system';

import { ServiceHierarchyHealth } from 'api/data-contracts';
import { ChildServiceContainer } from 'styles';
import HealthCheckList from './HealthCheckList';

const ChildService: React.FC<{
  servicePath?: string | null,
  childService: ServiceHierarchyHealth,
  services: ServiceHierarchyHealth[]
}> =
  ({ servicePath, childService, services }) => {
    return (
      <div style={{ ...ChildServiceContainer }}>
        <AccordionItem heading={childService.name}>
          <div>
            {childService.healthChecks ? (
              <div>
                <HealthCheckList rootServiceName={`${servicePath}/${childService.name}`} healthChecks={childService.healthChecks}/>
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
                      <ChildService servicePath={`${servicePath}/${childService.name}`} childService={child} services={services}/>
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
