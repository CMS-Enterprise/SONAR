import { useTheme } from '@emotion/react';
import React from 'react';
import { TenantHealth } from 'api/data-contracts';
import HealthStatusBadge from '../../Badges/HealthStatusBadge';
import {
  getTenantItemSpanStyle,
  getTenantItemStyle,
} from './TenantItem.Style';

const TenantItem: React.FC<{
  tenant: TenantHealth,
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
            <span css={getTenantItemSpanStyle(theme)}>
              {tenant.tenantName}: {rs.displayName}
            </span>
          </div>
        )}
      </div>
    )
  };

export default TenantItem;
