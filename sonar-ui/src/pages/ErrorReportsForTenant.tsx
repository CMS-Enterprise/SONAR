import { Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import { parentContainerStyle } from 'App.Style';
import { getEmptyTableMessageStyle } from 'components/ApiKeys/ApiKeyTable.Style';
import Breadcrumbs from 'components/Services/Breadcrumbs/Breadcrumbs';
import { useListErrorReportsForTenant } from 'components/ErrorReports/ErrorReports.Hooks';
import ErrorReportsTable from 'components/ErrorReports/ErrorReportsTable';
import { BreadcrumbContext } from 'components/Services/Breadcrumbs/BreadcrumbContext';
import { useMaybeGetHierarchyConfigQuery } from 'components/Services/Services.Hooks';
import { getServiceRootStatusAndName } from 'helpers/ServiceHelper';
import { getService } from 'helpers/ServiceHierarchyHelper';

const ErrorReportsForTenant = () => {
  const theme = useTheme();

  const params = useParams();
  const environmentName = params.environment as string;
  const tenantName = params.tenant as string;

  const { serviceName } = getServiceRootStatusAndName(params['*'] || '');
  const hierarchyConfigQuery = useMaybeGetHierarchyConfigQuery(environmentName, tenantName);
  const serviceIsValid = hierarchyConfigQuery.data ?
    (getService(serviceName, hierarchyConfigQuery.data.services)? true : false) :
    false;

  const [searchParams] = useSearchParams();
  const query = {
    serviceName: serviceName,
    start: searchParams.get('start') ?? undefined,
    end: searchParams.get('end') ?? undefined
  };

  const { isLoading, data } = useListErrorReportsForTenant(environmentName, tenantName, query);
  const errorReports = data ?? [];

  return (
    <section className="ds-l-container" css={parentContainerStyle}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%" }}>
        { (serviceName.length > 0) ? (
            serviceIsValid ? (
                <BreadcrumbContext.Provider value={{
                  serviceHierarchyConfiguration: hierarchyConfigQuery.data!,
                  errorReportsCount: errorReports.length
                }}>
                  <Breadcrumbs/>
                </BreadcrumbContext.Provider>) :
              <div>Invalid Service: {serviceName}</div>
          ) :
          <Breadcrumbs />
        }
      </div>
      { isLoading ? (<Spinner />) : (
        errorReports.length > 0 ?
          <ErrorReportsTable errorReports={errorReports} /> :
          <div className='ds-u-margin-top--1' css={getEmptyTableMessageStyle(theme)}>
            There are 0 associated error reports.
          </div>
      )}
    </section>
  )
}

export default ErrorReportsForTenant;
