import { Theme } from '@emotion/react';

export const LightTheme : Theme = {
  name: 'Light',
  textColor: '#5A5A5A',
  foregroundColor: '#FFF',
  highlightColor: '#F2F2F2',
  backgroundColor: '#F2F2F2',
  accentColor: '#0071BC',
  accentShadedColor: '#005289',
  maintenanceBannerTextColor: '#262626',
  sonarColors: {
    sonarGreen: '#12890E',
    sonarGrey: '#D9D9D9',
    sonarGold: '#F9CA35',
    sonarOrange: '#F89D0B',
    sonarRed: '#E31C3D',
    sonarYellow: "#eed202"
  }
}

export const DarkTheme : Theme = {
  name: 'Dark',
  textColor: '#F2F2F2',
  foregroundColor: '#262626',
  highlightColor: '#5A5A5A',
  backgroundColor: '#393E46',
  accentColor: '#FFD369',
  accentShadedColor: '#D8B258',
  maintenanceBannerTextColor: '#262626',
  sonarColors: {
    sonarGreen: '#12890E',
    sonarGrey: '#D9D9D9',
    sonarGold: '#F9CA35',
    sonarOrange: '#F89D0B',
    sonarRed: '#E31C3D',
    sonarYellow: "#eed202"
  }
}
