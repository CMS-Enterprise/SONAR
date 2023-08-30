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
  ServiceOverviewContentStyle,
  ServiceOverviewHeaderStyle
} from './ServiceOverview.Style';
import ExternalLinkIcon from 'components/Icons/ExternalLinkIcon';

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
          { serviceConfig && (
            <>
              { serviceConfig.description && (
                <div>
                  <div css={ServiceOverviewHeaderStyle}> Description </div>
                  <span css={ServiceOverviewContentStyle}> {serviceConfig.description} </span>
                </div>
              )}
              {serviceConfig.url && (
                <div>
                  <div css={ServiceOverviewHeaderStyle}> Uri </div>
                  <a css={ServiceOverviewContentStyle} target='_blank' rel="noreferrer" href={serviceConfig.url}>
                    {serviceConfig.url}&nbsp;
                    <ExternalLinkIcon className='ds-u-font-size--sm'/>
                  </a>
                </div>
              )}
            </>

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
              const childSvcHealth = serviceHealth?.children?.find(
                childObj => childObj.name === child);
              return (
              <div key={child} css={getSubContainerStyle}>
                <div css={[getSubsectionContainerStyle, DynamicTextFontStyle]}>
                  {childSvcHealth && childSvcHealth.aggregateStatus && (
                    <span>
                      <HealthStatusBadge theme={theme} status={childSvcHealth.aggregateStatus} />
                    </span>
                  )}
                  {childSvcHealth && (
                    <span css={childSvcHealth && getBadgeSpanStyle}>
                      <Link to={location.pathname + "/" + child}>
                        { childSvcHealth.displayName }
                      </Link>
                    </span>
                  )}
                </div>
              </div>
            )})}
          </>
          : null}
      </div>
    )
  }

export default ServiceOverview;
