import { css, Theme } from '@emotion/react';

export function mainStyle(theme: Theme) {
  return css({
    position: 'absolute',
    height: '100%',
    width: '100%',
    backgroundColor: theme.backgroundColor,
    color: theme.textColor,
    'a,a:focus': {
      textDecoration: 'none !important'
    },
    '--link__color': theme.textColor,
    '--link__color--hover': theme.accentColor,
    '--link__color--visited': theme.textColor
  });
}
