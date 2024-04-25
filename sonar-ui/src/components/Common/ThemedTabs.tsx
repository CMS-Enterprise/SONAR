import { Tabs } from '@cmsgov/design-system';
import { TabsProps } from '@cmsgov/design-system/dist/types/Tabs/Tabs';
import { useTheme } from '@emotion/react';
import React from 'react';
import { getTabsStyle } from './ThemedTabs.Style';

const ThemedTabs = (props: TabsProps) => {
  const theme = useTheme();

  return (
    <div css={getTabsStyle(theme)}>
      <Tabs
        {...props}
      />
    </div>

  )
}

export default ThemedTabs;
