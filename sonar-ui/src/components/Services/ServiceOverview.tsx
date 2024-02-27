import { useTheme } from '@emotion/react';
import {  DateTimeHealthStatusValueTuple, ServiceHierarchyHealth} from 'api/data-contracts';
import ExternalLinkIcon from 'components/Icons/ExternalLinkIcon';
import React, { useContext } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { DynamicTextFontStyle } from '../../App.Style';
import HealthStatusBadge from '../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../Badges/HealthStatusBadge.Style';
import ServiceAlertsModule from './ServiceAlerts/ServiceAlertsModule';
import HealthCheckList from './HealthStatus/HealthCheckList';
import {
  getServiceOverviewStyle,
  getSubContainerStyle,
  getSubsectionContainerStyle,
  ServiceOverviewContentStyle,
  ServiceOverviewHeaderStyle
} from './ServiceOverview.Style';
import ServiceTagsTable from './ServiceTags/ServiceTagsTable';
import ServiceVersionModule from './ServiceVersion/ServiceVersionModule';
import StatusHistoryModule from './StatusHistory/StatusHistoryModule';
import { ServiceOverviewContext } from './ServiceOverviewContext';

const ServiceOverview: React.FC<{
  serviceHealth: ServiceHierarchyHealth,
  servicePath: string,
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string
}> =
  ({
    serviceHealth,
    servicePath,
    addTimestamp,
    closeDrawer,
    selectedTileId
  }) => {
    const context = useContext(ServiceOverviewContext)!;
    const serviceConfiguration = context.serviceConfiguration;
    const location = useLocation();
    const theme = useTheme();
    return (
      <div css={getServiceOverviewStyle(theme)}>
        <div>
          <ServiceAlertsModule />
        </div>
        <div>
          { ((context.serviceVersionDetails != null) && (context.serviceVersionDetails.length >= 1) &&
            <ServiceVersionModule/>
          )}
        </div>
        <div>
          { serviceHealth && (
            <StatusHistoryModule
              addTimestamp={addTimestamp}
              closeDrawer={closeDrawer}
              selectedTileId={selectedTileId}
              servicePath={servicePath}
              serviceHealth={serviceHealth}
            />
          )}
        </div>
        <div>
          { serviceConfiguration && (
            <>
              { serviceConfiguration.description && (
                <div>
                  <div css={ServiceOverviewHeaderStyle}> Description </div>
                  <span css={ServiceOverviewContentStyle}> {serviceConfiguration.description} </span>
                </div>
              )}
              {serviceConfiguration.url && (
                <div>
                  <div css={ServiceOverviewHeaderStyle}> Uri </div>
                  <a css={ServiceOverviewContentStyle} target='_blank' rel="noreferrer" href={ serviceConfiguration.url}>
                    {serviceConfiguration.url}&nbsp;
                    <ExternalLinkIcon className='ds-u-font-size--sm'/>
                  </a>
                </div>
              )}
            </>
          )}
        </div>
        <div>
          {serviceHealth.tags && Object.entries(serviceHealth.tags).length > 0 && (
            <ServiceTagsTable tags={serviceHealth.tags} />
          )}
        </div>
        <div>
          { serviceConfiguration && serviceHealth && (
            <HealthCheckList
              healthCheckStatuses={serviceHealth.healthChecks}
            />
          )}
        </div>
        { serviceConfiguration && serviceConfiguration.children && serviceConfiguration.children.length > 0 ?
          <>
            <div css={ServiceOverviewHeaderStyle}>
              Services
            </div>
            { serviceConfiguration.children.map(child => {
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
                        { childSvcHealth.displayName}
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
