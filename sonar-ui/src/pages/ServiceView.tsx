import React, { useEffect, useState } from 'react';

import { ProblemDetails, ServiceHierarchyHealth } from 'api/data-contracts';
import RootService from 'components/ServiceListView/RootService';
import { createSonarClient } from 'helpers/ApiHelper';
import { HttpResponse } from 'api/http-client';

const ServiceView = () => {
  const [services, setServices] = useState<ServiceHierarchyHealth[] | null>(null);

  useEffect(() => {
    // create sonar client
    const sonarClient = createSonarClient();

    sonarClient.getServiceHierarchyHealth('foo', 'baz')
      .then((res: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        setServices(res.data);
      })
      .catch((e: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        console.log(`Error fetching health metrics: ${e.error}`);
      });
  }, []);

  return services ? (
    <section className="ds-l-container">
      <div>
        {services.map(rootService => (
          <div key={rootService.name}>
            <RootService rootService={rootService} services={services}/>
          </div>))}
      </div>
    </section>
  ) : null
}

export default ServiceView;
