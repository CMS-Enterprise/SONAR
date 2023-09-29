import React, { useContext } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { DynamicTextFontStyle } from '../../../App.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';
import {
  getBreadcrumbsStyle,
  crumbStyle
} from './Breadcrumbs.Style'

const Breadcrumbs: React.FC<{
  environmentName: string,
  tenantName: string
}> =
  ({ environmentName, tenantName }) => {
    const serviceContext  = useContext(ServiceOverviewContext);
    const location = useLocation()

    let currentServiceLink = ``

    const environmentIndex = 0;
    const tenantIndex = 2;
    const serviceIndexStart = 4;
    const serviceCrumbs = location.pathname.split(`/`)
      .filter(crumb => (crumb !== ''))
      .map((crumb, index, array) => {
        const displayName = serviceContext!.serviceHierarchyConfiguration.services.filter(svc => crumb === svc.name)
          ?.map(svc => {
            return svc.displayName
          })

        currentServiceLink += `/${crumb}`;

        let displaySpan;

        if (index === environmentIndex) {
          displaySpan = <span key={crumb} css={crumbStyle}>Environment:
            <Link to={currentServiceLink}> {crumb}</Link>
          </span>;
        } else if (index === tenantIndex) {
          displaySpan = <span key={crumb} css={crumbStyle}>Tenant:
            <Link to={currentServiceLink}> {crumb}</Link>
          </span>;
        } else if (index >= serviceIndexStart) {
          if (index === (array.length - 1)) {
            displaySpan = <span key={crumb} css={crumbStyle}>
              <Link to={currentServiceLink}>{crumb}</Link>
            </span>;
          } else {
            displaySpan = (
              <span key={crumb} css={crumbStyle}>
                <Link to={currentServiceLink}>{displayName}</Link>
              </span>
            );
          }
        }
        return displaySpan;
      })

    return (
      <div
        css={[getBreadcrumbsStyle, DynamicTextFontStyle]}
        data-test="breadcrumbs"
      >
        {serviceCrumbs}
      </div>
    )
  };

export default Breadcrumbs;
