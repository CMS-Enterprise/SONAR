import { SerializedStyles, Theme, css } from "@emotion/react";

export const secondaryActionButtonStyle : (theme: Theme) => SerializedStyles = theme => css({
  color: theme.textColor,
  borderRadius: '.625rem',
  backgroundColor: 'none',
  '&:hover, &:focus, &:focus:hover': {
    color: theme.textColor,
    backgroundColor: 'transparent',
  },
});
