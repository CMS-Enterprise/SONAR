import React, { useContext } from 'react';
import { ServiceOverviewHeaderStyle } from '../ServiceOverview.Style';
import { ServiceVersionItemStyle } from './ServiceVersion.Style';
import { ServiceOverviewContext } from '../ServiceOverviewContext';

const ServiceVersionModule: React.FC = () => {
  const context = useContext(ServiceOverviewContext)!;
  return (
    <>
      <div css={ServiceOverviewHeaderStyle}>
        Version
      </div>
      {context.serviceVersionDetails.map(function(data) {
          return (
            <div css={ServiceVersionItemStyle}>
              {data.versionType} : {data.version}
            </div>
          )
        }
      )}
    </>
  );
};

export default ServiceVersionModule;
