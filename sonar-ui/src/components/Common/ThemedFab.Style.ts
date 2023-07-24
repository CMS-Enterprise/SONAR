import { css, SerializedStyles, Theme } from '@emotion/react';

export const getFabStyle : (theme: Theme) => SerializedStyles = theme => css({
  width: '3.1875rem',
  height: '2.75rem',
  padding: '.625rem',
  borderRadius: '.625rem',
  position: 'fixed',
  zIndex: 9999,
  bottom: 24,
  right: 24,
  justifyContent: 'center',
  alignItems: 'center',
  display: 'flex',
  flexShrink: 0,
  border: `1px solid ${theme.accentColor}`,
  color: theme.accentColor,
  '&:hover, &:focus, &:focus:hover': {
    color: theme.backgroundColor,
    backgroundColor: theme.accentShadedColor
  },
});

