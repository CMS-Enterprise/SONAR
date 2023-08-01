import { css, SerializedStyles, Theme } from '@emotion/react';
export const getDropdownStyle : (theme: Theme) => SerializedStyles = theme => css({
  '& select': {
    borderRadius: ".4375rem",
    border: '1px solid #5A5A5A',
    backgroundColor: theme.backgroundColor,
    color: theme.textColor,
    whiteSpace: "pre-wrap"
  },
  '& label': {
    marginTop: 12,
    whiteSpace: "pre-wrap"
  }
});
