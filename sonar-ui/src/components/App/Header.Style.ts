import { css, CSSObject, SerializedStyles, Theme } from '@emotion/react';
import { CSSProperties } from 'react';
import { DarkTheme, LightTheme } from '../../themes';
import { StaticTextFontStyle } from '../../App.Style';

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
  alignItems: 'center',
  fontFamily: "'Zen Tokyo Zoo', cursive"
}

export const SiteTitleStyle: CSSObject = {
  fontSize: '3rem'
}

export const NavBarRightSideStyle: SerializedStyles = css({
  '*': {
    verticalAlign: 'middle'
  }
});

export const NavLinkStyle: CSSObject = {
  fontSize: '1.5rem',
  marginRight: '40px'
};

export const GetThemeToggleLabelStyle: (theme: Theme) => SerializedStyles = theme => css({
  ...StaticTextFontStyle,
  cursor: 'pointer',
  fontSize: '0.75rem',
  '&:hover': {
    color: theme.accentColor
  }
})

export const ThemeToggleStyle: SerializedStyles = css({
  marginLeft: '0.5rem',
  top: '-2px',
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
