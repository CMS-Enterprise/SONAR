import React from 'react';
import { useEffect, useState } from 'react';
import { AccordionItem, Spinner, SvgIcon, AlertCircleIcon, InfoCircleIcon, WarningIcon, CheckCircleIcon} from '@cmsgov/design-system';
import { TenantHealth, HealthStatus } from 'api/data-contracts';
import { getHealthStatusClass } from 'helpers/ServiceHierarchyHelper';

const TenantItem: React.FC<{
  tenant: TenantHealth,
  open: string | null,
  selected: boolean,
  setOpen: (value: string | null) => void,
  statusColor: string
}> =
  ({ tenant, open, selected, setOpen, statusColor }) => {
    const [loading, setLoading] = useState(true);

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
          <div className='tenantItem'>
            <span>{renderIconSelection(rs.aggregateStatus)}</span>
            <span style={{ verticalAlign:'middle', paddingLeft:'2px' }}>{tenant.tenantName}: {rs.displayName}</span>
          </div>
        )}
      </div>
    )
  };

export default TenantItem;
