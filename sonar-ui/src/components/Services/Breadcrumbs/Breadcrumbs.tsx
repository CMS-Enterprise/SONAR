import React, { useContext } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { crumbStyle, crumbDelimiterStyle } from './Breadcrumbs.Style'
import { BreadcrumbContext } from './BreadcrumbContext';
import ErrorReportsCount from 'components/ErrorReports/ErrorReportsCount';

const Breadcrumbs: React.FC = () => {
  const breadcrumbContext = useContext(BreadcrumbContext);
  const errorReportsCount = breadcrumbContext?.errorReportsCount ?? 0;

  const location = useLocation();
  const pageIsErrorReports = location.pathname.includes('error-reports');

  const hierarchyPath = location.pathname.includes('environments') ?
    location.pathname.split('environments/')[1] :
    location.pathname;

  let currentServiceLink = ``

  const environmentIndex = 0;
  const tenantIndex = 2;
  const serviceIndexStart = 4;
  const serviceCrumbs = hierarchyPath.split(`/`)
    .filter(crumb => (crumb !== ''))
    .map((crumb, index, array) => {
      const displayName = breadcrumbContext ? breadcrumbContext.serviceHierarchyConfiguration?.services.filter(svc => crumb === svc.name)
        .map(svc => {
          return svc.displayName
        }) : null

      currentServiceLink += `/${crumb}`;

      let breadcrumbLink;

      if (index === environmentIndex) {
        breadcrumbLink = <Link key={'env:' + crumb} to={currentServiceLink}>{crumb}</Link>;
      } else if (index === tenantIndex) {
        breadcrumbLink = <Link key={'tnt:' + crumb} to={currentServiceLink}>Tenant: {crumb}</Link>;
      } else if (index >= serviceIndexStart) {
        breadcrumbLink = <Link key={'svc:' + crumb} to={currentServiceLink}> {displayName}</Link>;
      }

      return breadcrumbLink;
    })

  return (
    <div css={crumbStyle}>
      <span css={crumbDelimiterStyle}>
        <Link to={'/'}>Environments</Link>
        {serviceCrumbs}
      </span>
      { pageIsErrorReports ?
        ' / Error Reports' :
        <ErrorReportsCount
          errorReportsPath={location.pathname}
          errorReportsCount={errorReportsCount}
        />
      }
    </div>
  )
};

export default Breadcrumbs;
