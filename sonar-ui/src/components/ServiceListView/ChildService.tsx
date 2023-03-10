import React from 'react';
import { ServiceHierarchyHealth } from "../../api/data-contracts";
import HealthCheckList from "./HealthCheckList";
import { ChildServiceContainer, HeadingContainer } from "../../styles";

const ChildService: React.FC<{
  childService: ServiceHierarchyHealth,
  services: ServiceHierarchyHealth[]
}> =
  ({ childService, services }) => {
    return (
      <div style={{ ...ChildServiceContainer }}>
        <div style={HeadingContainer}>
          {childService.name}
        </div>
        <div>
          {childService.healthChecks ? (
            <div>
              <HealthCheckList healthChecks={childService.healthChecks}/>
            </div>
          ) : null}
          {childService.children && childService.children.length > 0 ?
            <>
              <div style={HeadingContainer}>
                Services:
              </div>
              <ul>
                {childService.children.map(child => (
                  <li key={child.name}>
                    <ChildService childService={child} services={services}/>
                  </li>
                ))}
              </ul>
            </> : null}
        </div>
      </div>
    )
  }

export default ChildService;
