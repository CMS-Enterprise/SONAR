import { Tooltip, TooltipIcon } from '@cmsgov/design-system';
import { TooltipProps } from '@cmsgov/design-system/dist/types/Tooltip/Tooltip';
import { useTheme } from '@emotion/react';
import React from 'react';
import { getThemedToolTipStyle } from './ThemedToolTip.Style';

const ThemedInlineTooltip: React.FC<{
  title: string,
  placement?: TooltipProps["placement"]
}> = ({ title, placement }) => {
  const theme = useTheme();

  return (
    <Tooltip
      css={getThemedToolTipStyle(theme)}
      title={title}
      children={<TooltipIcon />}
      placement={placement}
      />
  )
}

export default ThemedInlineTooltip;
