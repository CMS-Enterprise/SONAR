import React from 'react';
import { AccordionItem, Spinner } from '@cmsgov/design-system';
import { EnvironmentHealth, TenantHealth } from 'api/data-contracts';
import { getHealthStatusClass } from 'helpers/ServiceHierarchyHelper';
import { createSonarClient } from 'helpers/ApiHelper';
import TenantItem from 'components/Tenant/TenantItem';
import { useQuery } from 'react-query';

const EnvironmentItem: React.FC<{
  environment: EnvironmentHealth,
  open: string | null,
  selected: boolean,
  setOpen: (value: string | null) => void,
  statusColor: string
}> =
  ({ environment, open, selected, setOpen, statusColor }) => {
    const sonarClient = createSonarClient();

    const { isLoading, isError, data, error } = useQuery<TenantHealth[], Error>({
      queryKey: ["tenantHealth"],
      enabled: selected,
      queryFn: () => sonarClient.getTenants()
        .then((res) => {
          return res.data;
        })
    })

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
                     buttonClassName={getHealthStatusClass(environment.aggregateStatus)}>
        {
          isLoading ? (<Spinner />) :
          data?.filter(t=> t.environmentName === environment.environmentName)
            .map(t =>
              <TenantItem key={t.tenantName}
                          tenant={t}/>
            )
        }
      </AccordionItem>
    );
  };

export default EnvironmentItem;
