import { css, SerializedStyles, Theme } from '@emotion/react';
export const getDialogStyle : (theme: Theme) => SerializedStyles = theme => css({
  color: theme.textColor,
  backgroundColor: theme.backgroundColor,
  borderRadius: '10px',
  boxShadow: "none",
  '& header': {
    borderBottom: `3px solid ${theme.accentColor}`,
    justifyContent: "center",
    '& button': {
      display: "none"
    }
  }
});
