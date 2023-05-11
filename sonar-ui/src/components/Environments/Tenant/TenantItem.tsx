import { useTheme } from '@emotion/react';
import React from 'react';
import { AlertCircleIcon, WarningIcon, CheckCircleIcon} from '@cmsgov/design-system';
import { TenantHealth, HealthStatus } from 'api/data-contracts';
import {
  getIconStyle,
  TenantItemSpanStyle,
  TenantItemStyle
} from './TenantItem.Style';

const TenantItem: React.FC<{
  tenant: TenantHealth,
}> =
  ({tenant}) => {
    const theme = useTheme();
    const renderIconSelection = (aggregateStatus: HealthStatus | undefined) => {
      switch (aggregateStatus) {
        case HealthStatus.Online:
          return <CheckCircleIcon css={getIconStyle(aggregateStatus, theme)} />;
          break;
        case HealthStatus.AtRisk:
          return <AlertCircleIcon css={getIconStyle(aggregateStatus, theme)} /> ;
          break;
        case HealthStatus.Degraded:
          return <AlertCircleIcon css={getIconStyle(aggregateStatus, theme)} /> ;
          break;
        case HealthStatus.Offline:
          return <WarningIcon css={getIconStyle(aggregateStatus, theme)} /> ;
          break;
        case HealthStatus.Unknown:
        default:
          return <WarningIcon css={getIconStyle(aggregateStatus, theme)} /> ;
          break;
      }
    }

    return (
      <div>
        {tenant.rootServices?.map(rs =>
          <div css={TenantItemStyle} key={rs.name}>
            <span>{renderIconSelection(rs.aggregateStatus)}</span>
            <span css={TenantItemSpanStyle}>
              {tenant.tenantName}: {rs.displayName}
            </span>
          </div>
        )}
      </div>
    )
  };

export default TenantItem;
