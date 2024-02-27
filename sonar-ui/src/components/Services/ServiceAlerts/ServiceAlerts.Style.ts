import { css, CSSObject, Theme } from '@emotion/react';

export const ServiceAlertsContainerStyle: CSSObject = {
  paddingTop: 10,
  paddingLeft: 10
}
export function getAlertIconStyle(theme: Theme, isFiring: boolean) {
  return css({
    color: isFiring ?
      theme.sonarColors.sonarRed : theme.sonarColors.sonarGreen,
    paddingBottom: 5,
    paddingLeft: 5,
    paddingRight: 5,
    width: "1.3rem"
  });
}

export const SilenceIconStyle: CSSObject = {
  paddingTop: 3
}
