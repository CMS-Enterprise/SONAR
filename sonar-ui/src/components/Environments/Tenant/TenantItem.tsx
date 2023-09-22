import { useTheme } from '@emotion/react';
import React from 'react';
import { Link } from 'react-router-dom';
import { TenantInfo } from 'api/data-contracts';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import { getBadgeSpanStyle } from '../../Badges/HealthStatusBadge.Style';
import VersionInfo from '../../Common/VersionInfo';
import { getTenantItemStyle } from './TenantItem.Style';

const TenantItem: React.FC<{
  tenant: TenantInfo,
}> =
  ({tenant}) => {
    const theme = useTheme();

    return (
      <div>
        {tenant.rootServices?.map(rs =>
          <div css={getTenantItemStyle(theme)} key={rs.name}>
            <span>
              <HealthStatusBadge theme={theme} status={rs.aggregateStatus} />
            </span>
            <span
              css={getBadgeSpanStyle(theme)}
              data-test="env-view-tenant"
            >
              <Link to={ "/" + tenant.environmentName +
                         "/tenants/" + tenant.tenantName +
                         "/services/" + rs.name }>
                {tenant.tenantName}: {rs.displayName}
              </Link>
            </span>
            {
              rs.versions && rs.versions.length &&
              <span css={getBadgeSpanStyle(theme)} data-test="env-view-tenant">
                <VersionInfo versions={rs.versions} />
              </span>
            }
          </div>
        )}
      </div>
    )
  };

export default TenantItem;
