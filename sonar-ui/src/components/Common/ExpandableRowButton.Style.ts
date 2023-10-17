import { SerializedStyles, Theme, css } from "@emotion/react";

export const expandableRowButtonStyle: (theme: Theme) => SerializedStyles = theme => css({
  color: theme.foregroundColor,
  backgroundColor: theme.accentColor,
  borderColor: theme.accentColor,
  '&:hover, &:focus, &:focus:hover': {
    color: theme.backgroundColor,
    backgroundColor: theme.accentShadedColor,
    borderColor: theme.accentShadedColor
  }
});
