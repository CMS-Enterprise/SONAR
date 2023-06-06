import { useTheme } from '@emotion/react';
import React from 'react';
import { Link, useLocation } from 'react-router-dom';

import {
  DateTimeHealthStatusValueTuple,
  ServiceConfiguration,
  ServiceHierarchyHealth,
} from 'api/data-contracts';
import { DynamicTextFontStyle, StaticTextFontStyle } from '../../App.Style';
import Breadcrumbs from './Breadcrumbs/Breadcrumbs';
import StatusHistoryModule from './StatusHistory/StatusHistoryModule';
import HealthCheckList from './HealthCheckList';
import {
  getServiceOverviewStyle,
  getSubContainerStyle,
  getSubsectionContainerStyle,
  ServiceOverviewHeaderStyle
} from './ServiceOverview.Style';

const ServiceOverview: React.FC<{
  environmentName: string,
  tenantName: string,
  serviceConfig: ServiceConfiguration,
  serviceHealth: ServiceHierarchyHealth,
  serviceConfigurationLookup: { [key: string]: ServiceConfiguration },
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string
}> =
  ({
    environmentName,
    tenantName,
    serviceConfig,
    serviceHealth,
    serviceConfigurationLookup,
    addTimestamp,
    closeDrawer,
    selectedTileId
  }) => {
    const location = useLocation();
    const theme = useTheme();

    return (
      <div css={getServiceOverviewStyle(theme)}>
        <Breadcrumbs
          environmentName={environmentName}
          tenantName={tenantName}
        />
        <div>
          <StatusHistoryModule
            addTimestamp={addTimestamp}
            closeDrawer={closeDrawer}
            selectedTileId={selectedTileId}
            servicePath={[serviceHealth.name]}
            serviceHealth={serviceHealth}
            environmentName={environmentName}
            tenantName={tenantName}
          />
        </div>
        <div>
          {serviceConfig.healthChecks ? (
            <HealthCheckList
              environmentName={environmentName}
              tenantName={tenantName}
              serviceConfig={serviceConfig}
              healthCheckStatuses={serviceHealth.healthChecks}
            />
          ) : null}
        </div>
        {serviceConfig.children && serviceConfig.children.length > 0 ?
          <>
            <div css={[ServiceOverviewHeaderStyle, StaticTextFontStyle]}>
              Services
            </div>
            {serviceConfig.children.map(child => (
              <div key={child} css={getSubContainerStyle}>
                <div css={[getSubsectionContainerStyle, DynamicTextFontStyle]}>
                  <Link to={location.pathname + "/" + child}>
                    {child}
                  </Link>
                </div>
              </div>
            ))}
          </>
          : null}
      </div>
    )
  }

export default ServiceOverview;