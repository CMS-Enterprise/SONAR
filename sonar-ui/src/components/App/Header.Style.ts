import { css, CSSObject, SerializedStyles } from '@emotion/react';
import { CSSProperties } from 'react';
import { DarkTheme, LightTheme } from '../../themes';

export const ToggleIconStyle: CSSProperties = {
  width: '16px',
  height: '16px',
  top: '-2px',
  position: 'relative'
}

export const NavBarStyle: CSSObject = {
  margin: '15px 50px',
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center'
}

export const SiteTitleStyle: CSSObject = {
  fontSize: '2rem',
  fontWeight: '700'
}

export const NavBarRightSideStyle: CSSObject = {
  display: 'flex',
  justifyContent: 'space-evenly',
  alignItems: 'center'
}

export const NavLinkStyle: CSSObject = {
  marginRight: '40px'
};

export const ThemeToggleStyle: SerializedStyles = css({
  '&.react-toggle--checked .react-toggle-track': {
    backgroundColor: LightTheme.backgroundColor,
    color: LightTheme.textColor
  },
  '.react-toggle-track': {
    backgroundColor: LightTheme.textColor,
    color: LightTheme.backgroundColor
  },
  '&.react-toggle--checked:hover .react-toggle-track': {
    backgroundColor: `${DarkTheme.accentColor} !important`,
    color: LightTheme.textColor
  },
  '&:hover .react-toggle-track': {
    backgroundColor: `${LightTheme.accentColor} !important`,
    color: LightTheme.backgroundColor
  },
  '&.react-toggle--checked .react-toggle-thumb': {
    boxShadow: 'none',
    borderColor: LightTheme.textColor
  },
  '.react-toggle-thumb': {
    boxShadow: 'none',
    borderColor: LightTheme.textColor
  }
});
