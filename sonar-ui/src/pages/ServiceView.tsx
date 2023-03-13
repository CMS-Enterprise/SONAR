import React, { useEffect, useState } from 'react';
import { SonarApi } from "api/SonarApi";
import { ServiceHierarchyHealth } from "../api/data-contracts";
import RootService from "../components/ServiceListView/RootService";

const ServiceView = () => {
  const [services, setServices] = useState<ServiceHierarchyHealth[] | null>(null);
  // create sonar client
  const sonarClient = new SonarApi({
    baseUrl: "http://localhost:8081"
  });

  useEffect(() => {
    sonarClient.getServiceHierarchyHealth("foo", "baz")
      .then((res) => {
        setServices(res.data);
      })
      .catch(e => console.log(`Error fetching health metrics: ${e.message}`));
  }, []);

  return services ? (
    <div>
      <div>
        {services.map(rootService => (
          <div key={rootService.name}>
            <RootService rootService={rootService} services={services}/>
          </div>))}
      </div>
    </div>
  ) : null
}

export default ServiceView;
