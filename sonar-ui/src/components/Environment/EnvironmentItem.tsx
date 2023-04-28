import React from 'react';
import { useEffect, useState } from 'react';
import { AccordionItem, Spinner } from '@cmsgov/design-system';
import { EnvironmentHealth, TenantHealth } from 'api/data-contracts';
import { getHealthStatusClass } from 'helpers/ServiceHierarchyHelper';
import { createSonarClient } from 'helpers/ApiHelper';
import TenantItem from 'components/Tenant/TenantItem';

const EnvironmentItem: React.FC<{
  environment: EnvironmentHealth,
  open: string | null,
  selected: boolean,
  setOpen: (value: string | null) => void,
  statusColor: string
}> =
  ({ environment, open, selected, setOpen, statusColor }) => {
    const [tenants, setTenants] = useState<TenantHealth[] | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
      if (selected) {
        const sonarClient = createSonarClient();
        sonarClient.getTenants()
          .then((res) => {
            setTenants(res.data);
            setLoading(false);
        })
          .catch(e => console.log(`Error fetching tenants: ${e.message}`));
      }
    }, [selected]);

    const handleToggle = () => {
      const expanded =
        open === environment.environmentName || environment.environmentName === undefined ?
          null : environment.environmentName;
      setOpen(expanded);
    }

    return (
      <AccordionItem heading={environment.environmentName}
                     isControlledOpen={selected}
                     onChange={handleToggle}
                     buttonClassName={getHealthStatusClass(environment.aggregateStatus, false)}>
        {
          loading ? (<Spinner />) :
          tenants?.filter(t=> t.environmentName === environment.environmentName)
            .map(t =>
              <TenantItem key={t.tenantName}
                          tenant={t}/>
            )
        }
      </AccordionItem>
    );
  };

export default EnvironmentItem;
