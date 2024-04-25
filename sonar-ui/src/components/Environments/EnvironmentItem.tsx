import { Accordion, AccordionItem, ArrowIcon, Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { EnvironmentHealth } from 'api/data-contracts';
import TenantItem from 'components/Environments/Tenant/TenantItem';
import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { EnvironmentItemContainerStyle, getEnvironmentStatusStyle } from './EnvironmentItem.Style';
import { useGetTenants } from './Environments.Hooks';

const EnvironmentItem: React.FC<{
  environment: EnvironmentHealth,
  openPanels: string[],
  setOpenPanels: (value: string[]) => void
}> =
  ({ environment, openPanels, setOpenPanels }) => {
    const theme = useTheme();
    const [expanded, setExpanded] = useState<boolean>(true);
    const { isLoading, data } = useGetTenants(expanded);

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
        getEnvironmentStatusStyle(theme, environment.isInMaintenance!),
      [environment.isInMaintenance, theme]);

    return (
      <div
        className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto"
        css={EnvironmentItemContainerStyle}
        data-test="env-view-accordion">
        <Accordion css={memoizedStyle}>
          <AccordionItem
            heading={
              <Link to={'/' + environment.environmentName}>
                {"Environment: " + environment.environmentName +
                  (environment.isInMaintenance ? " is currently undergoing maintenance" : "")}
              </Link>
            }
            isControlledOpen={expanded}
            onChange={handleToggle}
            closeIcon={<ArrowIcon direction={'up'} />}
            openIcon={<ArrowIcon direction={'down'} />}>
            {
              isLoading ? (<Spinner />) :
                data?.filter(t => t.environmentName === environment.environmentName)
                  .map(t =>
                    <TenantItem key={t.tenantName} tenant={t} includeHeading={true} />
                  )
            }
          </AccordionItem>
        </Accordion>
      </div>
    );
  };

export default EnvironmentItem;
