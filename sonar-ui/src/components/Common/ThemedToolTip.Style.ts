import { css, SerializedStyles, Theme } from '@emotion/react';

export const getThemedToolTipStyle : (theme: Theme) => SerializedStyles = theme => css({
  border: "none",
  backgroundColor: theme.foregroundColor,
  ".ds-c-tooltip-icon": {
    fill: theme.accentColor
  },
});
