import { Button } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { getAccordionToggleStyle } from './EnvironmentItem.Style';

const AccordionToggleAllButton: React.FC<{
  allPanelsOpen: boolean,
  handleToggle: (value: boolean) => void
}> =
  ({ allPanelsOpen, handleToggle }) => {
  const theme = useTheme();

  return (
    <div className="ds-l-row ds-u-justify-content--end">
      <div
        className="ds-l-sm-col--4 ds-u-margin-right--4 ds-u-margin-left--auto"
        css={getAccordionToggleStyle(theme)}
      >
        <Button
          variation="ghost"
          onClick={handleToggle}
        >
          {allPanelsOpen ? "Close All" : "Expand All"}
        </Button>
      </div>
    </div>
  )
}

export default AccordionToggleAllButton;
