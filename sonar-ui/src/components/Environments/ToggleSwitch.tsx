import { Button } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React, {} from "react";
import { getAccordionToggleStyle } from './EnvironmentItem.Style';

const ToggleSwitch: React.FC<{
  switchFlag: boolean,
  setSwitchFlag: (value: boolean) => void
}> =
({switchFlag, setSwitchFlag  }) => {
  const theme = useTheme();
  const handleClick = () => {
    setSwitchFlag(!switchFlag);
  }

  return (
      <div css={getAccordionToggleStyle(theme)}>
        <Button variation="ghost" onClick={handleClick}>
          <input checked={!switchFlag} type="checkbox" id="checkbox" onClick={handleClick} />
          <label>Hide Non-Production </label>
        </Button>
      </div>
  );
};

export default ToggleSwitch;
