import { css, SerializedStyles, Theme } from '@emotion/react';
export const getTextFieldStyle : (theme: Theme) => SerializedStyles = theme => css({
  '& input': {
    borderRadius: ".4375rem",
    border: '1px solid #5A5A5A',
    backgroundColor: theme.foregroundColor,
    color: theme.textColor
  }
});
