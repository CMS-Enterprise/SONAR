import '@emotion/react'

declare module '@emotion/react' {
  export interface Theme {
    name: string,
    foregroundColor: string,
    highlightColor: string,
    backgroundColor: string,
    accentColor: string,
    sonarColors: {
      sonarGreen: string,
      sonarGrey: string,
      sonarGold: string,
      sonarOrange: string,
      sonarRed: string
    }
  }

  // export * from '@emotion/react/types/index';
}
