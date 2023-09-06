import React from 'react';
import { ServiceVersionDetails } from '../../../api/data-contracts';
import { ServiceOverviewHeaderStyle } from '../ServiceOverview.Style';
import { ServiceVersionItemStyle } from './ServiceVersion.Style';

const ServiceVersionModule: React.FC<{
  serviceVersionDetails: ServiceVersionDetails[]
}> =
  ({ serviceVersionDetails}) => {

  return (
    <>
      <div css={ServiceOverviewHeaderStyle}>
        Version
      </div>
      <div>
        {serviceVersionDetails.map((item) => (
          <div css={ServiceVersionItemStyle}>
            {item.versionType}: {item.version}
          </div>
        ))}
      </div>
    </>

  );
};

export default ServiceVersionModule;
