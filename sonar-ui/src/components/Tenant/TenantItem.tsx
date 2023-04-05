import React from 'react';
import { AlertCircleIcon, WarningIcon, CheckCircleIcon} from '@cmsgov/design-system';
import { TenantHealth, HealthStatus } from 'api/data-contracts';

const TenantItem: React.FC<{
  tenant: TenantHealth,
}> =
  ({tenant}) => {
    const renderIconSelection = (aggregateStatus: HealthStatus | undefined) => {
      switch (aggregateStatus) {
        case HealthStatus.Online:
          return <CheckCircleIcon className="online-icon"/>;
          break;
        case HealthStatus.AtRisk:
          return <AlertCircleIcon className="atRisk-icon"/> ;
          break;
        case HealthStatus.Degraded:
          return <AlertCircleIcon className="degraded-icon"/> ;
          break;
        case HealthStatus.Offline:
          return <WarningIcon className='offline-icon'/> ;
          break;
        case HealthStatus.Unknown:
        default:
          return <WarningIcon className="unknown-icon"/> ;
          break;
      }
    }

    return (
      <div>
        {tenant.rootServices?.map(rs =>
          <div className='tenantItem' key={rs.name}>
            <span>{renderIconSelection(rs.aggregateStatus)}</span>
            <span style={{ verticalAlign:'middle', paddingLeft:'2px' }}>
              {tenant.tenantName}: {rs.displayName}
            </span>
          </div>
        )}
      </div>
    )
  };

export default TenantItem;
