import '@emotion/react'

declare module '@emotion/react' {
  export interface Theme {
    name: string,
    textColor: string,
    foregroundColor: string,
    highlightColor: string,
    backgroundColor: string,
    accentColor: string,
    accentShadedColor: string,
    maintenanceBannerTextColor: string,
    sonarColors: {
      sonarGreen: string,
      sonarGrey: string,
      sonarGold: string,
      sonarOrange: string,
      sonarRed: string,
      sonarYellow: string
    }
  }

  // export * from '@emotion/react/types/index';
}
