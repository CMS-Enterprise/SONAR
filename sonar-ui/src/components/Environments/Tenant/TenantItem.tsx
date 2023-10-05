import { Theme, useTheme } from '@emotion/react';
import React from 'react';
import { Link } from 'react-router-dom';
import { ServiceHierarchyInfo, TenantInfo } from 'api/data-contracts';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../../Badges/HealthStatusBadge.Style';
import VersionInfo from '../../Common/VersionInfo';
import { getTenantItemStyle } from './TenantItem.Style';

const TenantItem: React.FC<{
  tenant: TenantInfo,
  includeHeading?: boolean,
  flattenServices?: boolean
}> =
  ({ tenant, includeHeading, flattenServices }) => {
    const theme = useTheme();

    return (
      <div>
        {includeHeading &&
          <h3>
            <Link to={'/' + tenant.environmentName + '/tenants/' + tenant.tenantName}>{'Tenant: ' + tenant.tenantName}</Link>
          </h3>
        }
        {renderServiceList(theme, tenant, flattenServices)}
      </div>
    )
  };

function renderServiceList(theme: Theme, tenant: TenantInfo, flattenServices?: boolean) {
  if (!tenant.rootServices?.length) {
    return <></>;
  } else if (flattenServices) {
    return <>
      {
        Array.from(traverse(tenant.rootServices)).map(({ service, path }) =>
          <div css={getTenantItemStyle(theme)} key={service.name}>
            <span>
              <HealthStatusBadge theme={theme} status={service.aggregateStatus} />
            </span>

            <span
              css={getBadgeSpanStyle(theme)}
              data-test="env-view-tenant">
              <Link to={'/' + tenant.environmentName + '/tenants/' + tenant.tenantName + '/services/' + service.name}>
                {path.length ? path + ' / ' + service.displayName : service.displayName}
              </Link>
            </span>
            {
              service.versions && service.versions.length &&
              <span css={getBadgeSpanStyle(theme)} data-test="env-view-tenant">
                <VersionInfo versions={service.versions} />
              </span>
            }
          </div>
        )
      }
    </>
  } else {
    return <>
      {
        tenant.rootServices?.map(svc =>
          <div css={getTenantItemStyle(theme)} key={svc.name}>
            <span>
              <HealthStatusBadge theme={theme} status={svc.aggregateStatus} />
            </span>

            <span
              css={getBadgeSpanStyle(theme)}
              data-test="env-view-tenant">
              <Link to={'/' + tenant.environmentName + '/tenants/' + tenant.tenantName + '/services/' + svc.name}>
                {svc.displayName}
              </Link>
            </span>
            {
              svc.versions && svc.versions.length &&
              <span css={getBadgeSpanStyle(theme)} data-test="env-view-tenant">
                <VersionInfo versions={svc.versions} />
              </span>
            }
          </div>
        )
      }
    </>
  }
}

function* traverse(services: ServiceHierarchyInfo[]) {
  const queue = Array.from(services.map(svc => ({ service: svc, path: '' })));
  while (queue.length > 0) {
    const { service, path } = queue.shift()!;
    yield ({ service, path: path });
    if (service.children?.length) {
      queue.unshift(...service.children.map(svc => ({
        service: svc,
        path: path.length ? path + ' / ' + service.displayName : service.displayName
      })))
    }
  }
}


export default TenantItem;
