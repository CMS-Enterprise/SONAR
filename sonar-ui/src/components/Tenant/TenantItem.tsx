import React from 'react';
import { useEffect, useState } from 'react';
import { AccordionItem, Spinner } from '@cmsgov/design-system';

import { TenantHealth } from 'api/data-contracts';
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


    return tenant? (
      <div>
        <div>
          {

            <div>
              <div> Tenant Name: {tenant.tenantName},
                HealthStatus: {tenant.aggregateStatus?tenant.aggregateStatus:"Unknown"}</div>
            </div>

          }
        </div>
      </div>
    ): null
  };

export default TenantItem;
