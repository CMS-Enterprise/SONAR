import React, { useContext } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import { crumbStyle } from './Breadcrumbs.Style'

const Breadcrumbs: React.FC = () => {
    const serviceContext  = useContext(ServiceOverviewContext);
    const location = useLocation()

    let currentServiceLink = ``

    const environmentIndex = 0;
    const tenantIndex = 2;
    const serviceIndexStart = 4;
    const serviceCrumbs = location.pathname.split(`/`)
      .filter(crumb => (crumb !== ''))
      .map((crumb, index, array) => {
        const displayName = serviceContext ? serviceContext.serviceHierarchyConfiguration.services.filter(svc => crumb === svc.name)
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
        <Link to={'/'}>Environments</Link>
        {serviceCrumbs}
      </div>
    )
  };

export default Breadcrumbs;
