import { Accordion, AccordionItem, ArrowIcon, Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { EnvironmentHealth, TenantHealth } from 'api/data-contracts';
import TenantItem from 'components/Environments/Tenant/TenantItem';
import { createSonarClient } from 'helpers/ApiHelper';
import React, { useEffect, useMemo, useState } from 'react';
import { useQuery } from 'react-query';
import {
  EnvironmentItemContainerStyle,
  getEnvironmentStatusStyle,
} from './EnvironmentItem.Style';

const EnvironmentItem: React.FC<{
  environment: EnvironmentHealth,
  openPanels: string[],
  setOpenPanels: (value: string[]) => void
}> =
  ({ environment, openPanels, setOpenPanels }) => {
    const sonarClient = createSonarClient();
    const theme = useTheme();
    const [expanded, setExpanded] =  useState<boolean>(true);
    const { isLoading, isError, data, error } = useQuery<TenantHealth[], Error>({
      queryKey: ["tenantHealth"],
      enabled: expanded,
      queryFn: () => sonarClient.getTenants()
        .then((res) => {
          return res.data;
        })
    })

    // check if current accordion is open when openPanels changes.
    useEffect(() => {
      if (openPanels.includes(environment.environmentName)) {
        setExpanded(true)
      } else {
        setExpanded(false);
      }
    }, [openPanels, environment.environmentName]);

    const handleToggle = () => {
      if (expanded) {
        // close panel, remove from open list.
        setOpenPanels(openPanels.filter(e => e !== environment.environmentName));
      } else {
        // open panel, add to open list.
        setOpenPanels([...openPanels, environment.environmentName]);
      }
      setExpanded(!expanded);
    }

    const memoizedStyle = useMemo(() =>
      getEnvironmentStatusStyle(theme),
      [theme]);

    return (
      <div className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto" css={EnvironmentItemContainerStyle}>
        <Accordion bordered css={memoizedStyle}>
          <AccordionItem heading={environment.environmentName}
                         isControlledOpen={expanded}
                         onChange={handleToggle}
                         closeIcon={<ArrowIcon direction={"up"} />}
                         openIcon={<ArrowIcon direction={"down"} />}
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
