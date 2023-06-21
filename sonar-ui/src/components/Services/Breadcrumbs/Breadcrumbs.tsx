import React from 'react';
import { Link, useLocation } from 'react-router-dom';

import { DynamicTextFontStyle } from '../../../App.Style';
import {
  getBreadcrumbsStyle,
  crumbStyle
} from './Breadcrumbs.Style'

const Breadcrumbs: React.FC<{
  environmentName: string,
  tenantName: string
}> =
  ({ environmentName, tenantName }) => {
    const location = useLocation()

    let currentServiceLink = ``

    const environmentIndex = 0;
    const tenantIndex = 2;
    const serviceIndexStart = 4;
    const serviceCrumbs = location.pathname.split(`/`)
      .filter(crumb => (crumb !== ''))
      .map((crumb, index, array) => {
        currentServiceLink += `/${crumb}`;

        let displaySpan;

        if (index === environmentIndex) {
          displaySpan = <span key={crumb} css={crumbStyle}>Environment: {crumb}</span>;
        } else if (index === tenantIndex) {
          displaySpan = <span key={crumb} css={crumbStyle}>Tenant: {crumb}</span>;
        } else if (index >= serviceIndexStart) {
          if (index === (array.length - 1)) {
            displaySpan = <span key={crumb} css={crumbStyle}>{crumb}</span>;
          } else {
            displaySpan = (
              <span key={crumb} css={crumbStyle}>
                <Link to={currentServiceLink}>{crumb}</Link>
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
