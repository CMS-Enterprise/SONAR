import { Accordion, AccordionItem, ArrowIcon, Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { useMemo } from 'react';
import * as React from 'react';
import { Link, useParams } from 'react-router-dom';
import { EnvironmentItemContainerStyle, getEnvironmentStatusStyle} from '../components/Environments/EnvironmentItem.Style';
import { useGetTenants } from '../components/Environments/Environments.Hooks';
import { useListErrorReports } from 'components/ErrorReports/ErrorReports.Hooks';
import TenantItem from '../components/Environments/Tenant/TenantItem';
import Breadcrumbs from '../components/Services/Breadcrumbs/Breadcrumbs';
import { BreadcrumbContext } from 'components/Services/Breadcrumbs/BreadcrumbContext';

const Environment = () => {
  const params = useParams();
  const environmentName = params.environment as string;
  const theme = useTheme();
  const { isLoading, data: tenantsData } = useGetTenants(true);

  const { data: errorReportsData } = useListErrorReports(environmentName);
  const errorReportsCount = errorReportsData?.length ?? 0;

  const TenantItems = useMemo(() => {
    return (
      <>
        {
          isLoading ? (<Spinner />) :
            tenantsData?.filter(t=> t.environmentName === environmentName)
              .map(t =>
                <div key={t.tenantName}>
                  <Accordion bordered css={getEnvironmentStatusStyle(theme, t.isInMaintenance!)}>
                    <AccordionItem
                      heading={
                        <Link to={"/" + environmentName + "/tenants/" + t.tenantName}>
                          {"Tenant: " + t.tenantName +
                            (t.isInMaintenance ? " is currently undergoing maintenance." : "")}
                        </Link>
                      }
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
      </>
    )
  }, [environmentName, isLoading, tenantsData, theme]);

  return(
      <div
        className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto"
        css={EnvironmentItemContainerStyle}
        data-test="env-view-accordion"
      >
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%" }}>
          <BreadcrumbContext.Provider value={{
            serviceHierarchyConfiguration: null,
            errorReportsCount: errorReportsCount
          }}>
            <Breadcrumbs/>
          </BreadcrumbContext.Provider>
        </div>
        {TenantItems}
      </div>




  );
}

export default Environment;
