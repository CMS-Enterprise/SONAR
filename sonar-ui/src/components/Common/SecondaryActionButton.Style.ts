import { SerializedStyles, Theme, css } from "@emotion/react";

export const secondaryActionButtonStyle : (theme: Theme) => SerializedStyles = theme => css({
  color: theme.textColor,
  borderRadius: '7px',
});
