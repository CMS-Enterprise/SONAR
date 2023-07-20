import { SerializedStyles, Theme, css } from "@emotion/react";

export const getGhostActionButtonStyle: (theme: Theme) => SerializedStyles = theme => css({
  color: theme.textColor,
  borderRadius: '7px',
  border: 'none',
  padding: '15px',
  display: 'flex',
  alignItems: 'center',

  '&:hover, &:focus, &:focus:hover': {
    color: theme.accentColor,
    backgroundColor: theme.highlightColor
  },

  '& svg': {
    marginRight: '0.25rem'
  }
});
