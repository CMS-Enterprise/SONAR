import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import React from 'react';

const AdHocMaintenanceHeader: React.FC<{
  handleModalToggle: () => void
}> = ({handleModalToggle}) => {
  return (
    <div className="ds-l-row ds-u-justify-content--end">
      <div
        className="ds-l-col--4 ds-u-margin-right--auto ds-u-margin-left--0"
      >
      </div>
      <div
        className="ds-l-sm-col--4 ds-u-margin-right--0 ds-u-margin-left--auto ds-u-text-align--right"
      >
        <PrimaryActionButton
          onClick={handleModalToggle}
        >
          + Start Maintenance Window
        </PrimaryActionButton>
      </div>
    </div>
  )
}

export default AdHocMaintenanceHeader;
