import { CSSObject, Theme } from '@emotion/react';

export function mainStyle(theme: Theme): CSSObject {
  return {
    position: 'absolute',
    height: '100%',
    width: '100%',
    backgroundColor: theme.backgroundColor,
    color: theme.foregroundColor,
  }
}
