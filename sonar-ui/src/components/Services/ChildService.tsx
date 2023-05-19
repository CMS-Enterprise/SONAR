import React from 'react';
import { AccordionItem } from '@cmsgov/design-system';
import { ServiceConfiguration, ServiceHierarchyHealth } from 'api/data-contracts';
import { getChildServiceContainerStyle } from './ChildService.Style';
import HealthCheckList from './HealthCheckList';

const ChildService: React.FC<{
  environmentName: string,
  tenantName: string,
  servicePath: string[],
  service: ServiceConfiguration,
  serviceHealth: ServiceHierarchyHealth | null | undefined,
  serviceConfigurationLookup: { [key: string]: ServiceConfiguration },
}> =
  ({
    environmentName,
    tenantName,
    servicePath,
    service,
    serviceHealth,
    serviceConfigurationLookup
  }) => {
    return (
      <div css={getChildServiceContainerStyle()}>
        <AccordionItem heading={service.name}>
          <div>
            {service.healthChecks ? (
              <div>
                <HealthCheckList
                  environmentName={environmentName}
                  tenantName={tenantName}
                  service={service}
                  healthCheckStatuses={serviceHealth?.healthChecks} />
              </div>
            ) : null}
            {service.children && service.children.length > 0 ?
              <>
                <div className="ds-l-col">
                  Services:
                </div>
                <ul>
                  {service.children.map(child => (
                    <div key={child}>
                      <ChildService
                        environmentName={environmentName}
                        tenantName={tenantName}
                        servicePath={[...servicePath, child]}
                        service={serviceConfigurationLookup[child]}
                        serviceHealth={serviceHealth?.children && serviceHealth.children.filter(svc => svc.name === child)[0]}
                        serviceConfigurationLookup={serviceConfigurationLookup} />
                    </div>
                  ))}
                </ul>
              </> : null}
          </div>
        </AccordionItem>
      </div>
    )
  };

export default ChildService;
