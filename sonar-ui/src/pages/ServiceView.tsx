import { Button, Drawer } from '@cmsgov/design-system';
import React, { useEffect, useState } from 'react';

import { ProblemDetails, ServiceHierarchyHealth } from 'api/data-contracts';
import RootService from 'components/ServiceListView/RootService';
import { createSonarClient } from 'helpers/ApiHelper';
import { HttpResponse } from 'api/http-client';

const ServiceView = () => {
  const [services, setServices] = useState<ServiceHierarchyHealth[] | null>(null);
  const environmentName = 'foo';
  const tenantName = 'baz'
  const [showDrawer, setShowDrawer] = useState(false);
  const [selectedTileId, setSelectedTileId] = useState<string>("");
  const [selectedTileData, setSelectedTileData] = useState<any>(null);

  useEffect(() => {
    // create sonar client
    const sonarClient = createSonarClient();

    sonarClient.getServiceHierarchyHealth(environmentName, tenantName)
      .then((res: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        setServices(res.data);
      })
      .catch((e: HttpResponse<ServiceHierarchyHealth[], ProblemDetails | void>) => {
        console.log(`Error fetching health metrics: ${e.error}`);
      });
  }, []);

  useEffect(() => {
    if (selectedTileData) {
      setShowDrawer(true);
    } else {
      setShowDrawer(false);
    }
  }, [selectedTileData]);

  const addTimestamp = (tileData: any, tileId: string) => {
    console.log(tileData.status);
    setSelectedTileData(tileData);
    setSelectedTileId(tileId);
  }

  const closeDrawer = () => {
    setShowDrawer(false);
    setSelectedTileData(null);
    setSelectedTileId("");
  }

  return services ? (
    <section className="ds-l-container">
      {showDrawer && (
        <Drawer heading={"Selected Timestamps"} onCloseClick={closeDrawer}>
          {selectedTileData && (
            <div>
              {selectedTileData.timestamp}: {selectedTileData.status}
            </div>
          )}
        </Drawer>
      )}
      <div>
        {services.map(rootService => (
          <div key={rootService.name}>
            <RootService
              environmentName={environmentName}
              tenantName={tenantName}
              rootService={rootService}
              services={services}
              addTimestamp={addTimestamp}
              closeDrawer={closeDrawer}
              selectedTileId={selectedTileId}
            />
          </div>))}
      </div>
    </section>
  ) : null
}

export default ServiceView;
