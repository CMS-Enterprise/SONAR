import { Accordion, AccordionItem, Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { EnvironmentHealth, HealthStatus, TenantHealth } from 'api/data-contracts';
import TenantItem from 'components/Tenant/TenantItem';
import { createSonarClient } from 'helpers/ApiHelper';
import React, { useCallback, useMemo } from 'react';
import { useQuery } from 'react-query';
import { atRiskStyle, degradedStyle, offlineStyle, onlineStyle, unknownStyle } from './EnvironmentItem.Style';

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
    }, [open]);

    const getStyle = (status: HealthStatus | undefined) => {
      let style;
      switch (status) {
        case HealthStatus.Online:
          style = onlineStyle(theme);
          break;
        case HealthStatus.Offline:
          style = offlineStyle(theme);
          break;
        case HealthStatus.Unknown:
          style = unknownStyle(theme);
          break;
        case HealthStatus.Degraded:
          style = degradedStyle(theme);
          break;
        case HealthStatus.AtRisk:
          style = atRiskStyle(theme);
          break;
        default:
          style = unknownStyle(theme);
      }
      return style;
    }

    const memoizedStyle = useMemo(() => getStyle(environment.aggregateStatus), []);

    return (
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

    );
  };

export default EnvironmentItem;
