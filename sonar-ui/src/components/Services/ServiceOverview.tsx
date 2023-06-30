import { useTheme } from '@emotion/react';
import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  DateTimeHealthStatusValueTuple,
  ServiceConfiguration,
  ServiceHierarchyHealth,
} from 'api/data-contracts';
import { DynamicTextFontStyle } from '../../App.Style';
import HealthStatusBadge from '../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../Badges/HealthStatusBadge.Style';
import Breadcrumbs from './Breadcrumbs/Breadcrumbs';
import StatusHistoryModule from './StatusHistory/StatusHistoryModule';
import HealthCheckList from './HealthStatus/HealthCheckList';
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
  servicePath: string,
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string
}> =
  ({
    environmentName,
    tenantName,
    serviceConfig,
    serviceHealth,
    servicePath,
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
          { serviceHealth && (
            <StatusHistoryModule
              addTimestamp={addTimestamp}
              closeDrawer={closeDrawer}
              selectedTileId={selectedTileId}
              servicePath={servicePath}
              serviceHealth={serviceHealth}
              environmentName={environmentName}
              tenantName={tenantName}
            />
          )}
        </div>
        <div>
          { serviceConfig && serviceHealth && (
            <HealthCheckList
              serviceConfig={serviceConfig}
              healthCheckStatuses={serviceHealth.healthChecks}
            />
          )}
        </div>
        { serviceConfig && serviceConfig.children && serviceConfig.children.length > 0 ?
          <>
            <div css={ServiceOverviewHeaderStyle}>
              Services
            </div>
            {serviceConfig.children.map(child => {
              const childAggStatus = serviceHealth?.children?.find(
                childObj => childObj.name === child)?.aggregateStatus;

              return (
              <div key={child} css={getSubContainerStyle}>
                <div css={[getSubsectionContainerStyle, DynamicTextFontStyle]}>
                  {childAggStatus && (
                    <span>
                      <HealthStatusBadge theme={theme} status={childAggStatus} />
                    </span>
                  )}
                  <span css={childAggStatus && getBadgeSpanStyle}>
                    <Link to={location.pathname + "/" + child}>
                      {child}
                    </Link>
                  </span>
                </div>
              </div>
            )})}
          </>
          : null}
      </div>
    )
  }

export default ServiceOverview;
