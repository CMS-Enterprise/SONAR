import { SerializedStyles, Theme, css } from "@emotion/react";

export const primaryActionButtonStyle: (theme: Theme) => SerializedStyles = theme => css({
  borderRadius: '7px',

  color: theme.foregroundColor,
  backgroundColor: theme.accentColor,

  '&:hover, &:focus, &:focus:hover': {
    color: theme.backgroundColor,
    backgroundColor: theme.accentShadedColor
  },
  ':disabled': {
    backgroundColor: '#D0D0D0',
    color: '#5A5A5A'
  },
});
