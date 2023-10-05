import { Accordion, AccordionItem, ArrowIcon, Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import * as React from 'react';
import { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { EnvironmentItemContainerStyle, getEnvironmentStatusStyle} from '../components/Environments/EnvironmentItem.Style';
import { useGetTenants } from '../components/Environments/Environments.Hooks';
import TenantItem from '../components/Environments/Tenant/TenantItem';
import Breadcrumbs from '../components/Services/Breadcrumbs/Breadcrumbs';

const Environment = () => {
  const params = useParams();
  const environmentName = params.environment as string;
  const theme = useTheme();
  const { isLoading, data } = useGetTenants(true);
  const memoizedStyle = useMemo(() =>
      getEnvironmentStatusStyle(theme),
    [theme]);

  return(
      <div
        className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto"
        css={EnvironmentItemContainerStyle}
        data-test="env-view-accordion"
      >

        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%" }}>
          <Breadcrumbs/>
        </div>

        {
          isLoading ? (<Spinner />) :
            data?.filter(t=> t.environmentName === environmentName)
              .map(t =>
                <div key={t.tenantName}>
                  <Accordion bordered css={memoizedStyle}>
                    <AccordionItem heading={<Link to={"/" + environmentName + "/tenants/" + t.tenantName}>{"Tenant: " + t.tenantName}</Link>}
                      defaultOpen={true}
                      closeIcon={<ArrowIcon direction={"up"} />}
                      openIcon={<ArrowIcon direction={"down"} />}
                    >
                       <TenantItem tenant={t} />
                    </AccordionItem>
                  </Accordion>
                  <p/>
                </div>
              )
        }

      </div>




  );
}

export default Environment;
