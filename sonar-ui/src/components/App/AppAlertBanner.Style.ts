import { css, SerializedStyles, Theme } from '@emotion/react';

export const AlertBannerCloseIconStyle : (theme: Theme) => SerializedStyles = theme => css({
  position: "absolute",
  top: 0,
  right: 0,
  '&:hover, &:focus, &:focus:hover': {
    color: theme.textColor,
    backgroundColor: 'transparent',
  },

});
