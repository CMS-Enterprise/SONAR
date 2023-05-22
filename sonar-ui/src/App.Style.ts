import { css, CSSObject, Theme } from '@emotion/react';

export function mainStyle(theme: Theme) {
  return css({
    position: 'absolute',
    height: '100%',
    width: '100%',
    backgroundColor: theme.backgroundColor,
    overflowY: 'scroll',
    color: theme.textColor,
    'a,a:focus': {
      textDecoration: 'none !important',
      outline: '0 none'
    },
    '--color-focus-light': theme.backgroundColor,
    '--link__color': theme.textColor,
    '--link__color--active': theme.textColor,
    '--link__color--hover': theme.accentColor,
    '--link__color--visited': theme.textColor
  });
}

export const staticTextFontStyle: CSSObject = {
  fontFamily: 'Helvetica Neue, Helvetica, Arial, sans-serif'
};

export const dynamicTextFontStyle: CSSObject = {
  fontFamily: 'Verdana, Geneva, sans-serif'
};

export const accordionContainerStyle: CSSObject = {
  border: '1px #a6a6a6',
  borderRadius: '40px'
}
