import { Theme } from '@emotion/react';

export const LightTheme : Theme = {
  name: 'Light',
  foregroundColor: '#3e3e3e',
  highlightColor: '#3c8ccb',
  backgroundColor: '#fff',
  accentColor: '#e4e3e2',
  sonarColors: {
    sonarGreen: '#12890E',
    sonarGrey: '#808080',
    sonarGold: '#f8C41F',
    sonarOrange: '#F89D0B',
    sonarRed: '#E31C3D'
  }
}

export const DarkTheme : Theme = {
  name: 'Dark',
  foregroundColor: '#eee',
  highlightColor: '#fed368',
  backgroundColor: '#222831',
  accentColor: '#4b515a',
  sonarColors: {
    sonarGreen: '#12890E',
    sonarGrey: '#808080',
    sonarGold: '#f8C41F',
    sonarOrange: '#F89D0B',
    sonarRed: '#E31C3D'
  }
}
