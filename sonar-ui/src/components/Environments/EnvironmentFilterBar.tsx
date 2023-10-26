import { CloseIcon, TextInput } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import SecondaryActionButton from '../Common/SecondaryActionButton';
import {
  getCloseFilterButtonStyle,
  getSearchInputContainerStyle,
  getSearchInputStyle
} from './EnvironmentFilterBar.Style';

const EnvironmentFilterBar: React.FC<{
  setFilter: (value: string) => void,
  filter: string
}> =
  ({ setFilter, filter }) => {
    const theme = useTheme();
    return (
      <div className="ds-l-row" css={getSearchInputContainerStyle}>
        <div
          className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto">
          <div className={"ds-l-container"}>
            <div className="ds-l-row ds-u-justify-content--center">
              <div className="ds-l-sm-col--6 ">
                <TextInput
                  placeholder={"Filter by environment name"}
                  css={getSearchInputStyle(theme)}
                  type={"text"}
                  value={filter}
                  onChange={(e) => setFilter(e.target.value)} />
              </div>

              <div className="ds-l-sm-col--1 ds-u-lg-padding-left--1">
                <SecondaryActionButton css={getCloseFilterButtonStyle(theme)} onClick={() => setFilter("")}>
                  <CloseIcon />
                </SecondaryActionButton>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };

export default EnvironmentFilterBar;
