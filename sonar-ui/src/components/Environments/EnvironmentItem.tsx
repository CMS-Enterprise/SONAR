import { Accordion, AccordionItem, Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { EnvironmentHealth, TenantHealth } from 'api/data-contracts';
import TenantItem from 'components/Environments/Tenant/TenantItem';
import { createSonarClient } from 'helpers/ApiHelper';
import React, { useCallback, useMemo } from 'react';
import { useQuery } from 'react-query';
import {
  EnvironmentItemContainerStyle,
  getEnvironmentStatusStyle,
} from './EnvironmentItem.Style';

const EnvironmentItem: React.FC<{
  environment: EnvironmentHealth,
  open: string | null,
  selected: boolean,
  setOpen: (value: string | null) => void,
  statusColor: string
}> =
  ({ environment, open, selected, setOpen, statusColor }) => {
    const sonarClient = createSonarClient();
    const theme = useTheme();
    const { isLoading, isError, data, error } = useQuery<TenantHealth[], Error>({
      queryKey: ["tenantHealth"],
      enabled: selected,
      queryFn: () => sonarClient.getTenants()
        .then((res) => {
          return res.data;
        })
    })

    const handleToggle = useCallback(() => {
      const expanded =
        open === environment.environmentName || environment.environmentName === undefined ?
          null : environment.environmentName;
      setOpen(expanded);
    }, [open, environment]);

    const memoizedStyle = useMemo(() =>
      getEnvironmentStatusStyle(environment.aggregateStatus, theme),
      [environment.aggregateStatus, theme]);

    return (
      <div className="ds-l-sm-col--6 ds-l-md-col--4" css={EnvironmentItemContainerStyle}>
        <Accordion bordered css={memoizedStyle}>
          <AccordionItem heading={environment.environmentName}
                         isControlledOpen={selected}
                         onChange={handleToggle}
          >
            {
              isLoading ? (<Spinner />) :
                data?.filter(t=> t.environmentName === environment.environmentName)
                  .map(t =>
                    <TenantItem key={t.tenantName}
                                tenant={t}/>
                  )
            }
          </AccordionItem>
        </Accordion>
      </div>
    );
  };

export default EnvironmentItem;
