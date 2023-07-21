import { pageTitleStyle } from 'App.Style';
import PrimaryActionButton from 'components/Common/PrimaryActionButton';
import React from 'react';

const ApiKeyHeader: React.FC<{
  handleModalToggle: () => void
}> = ({handleModalToggle}) => {
  return (
    <div className="ds-l-row ds-u-justify-content--end">
      <div
        className="ds-l-col--4 ds-u-margin-right--auto ds-u-margin-left--0"
        css={pageTitleStyle}
      >
        Your Api Keys
      </div>
      <div
        className="ds-l-col--2 ds-u-margin-right--0 ds-u-margin-left--auto ds-u-text-align--right"
      >
        <PrimaryActionButton
          onClick={handleModalToggle}
        >
          + Create API Key
        </PrimaryActionButton>
      </div>
    </div>
  )
}

export default ApiKeyHeader;
