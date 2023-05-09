import { css, Theme } from '@emotion/react';

export function onlineStyle(theme: Theme) {
  return css({
    "--accordion__background-color": theme.sonarColors.sonarGreen,
    "--accordion__background-color--hover": theme.sonarColors.sonarGreen
  });
}

export function offlineStyle(theme: Theme) {
  return css({
    "--accordion__background-color": theme.sonarColors.sonarRed,
    "--accordion__background-color--hover": theme.sonarColors.sonarRed
  });
}

export function unknownStyle(theme: Theme) {
  return css({
    "--accordion__background-color": theme.sonarColors.sonarGrey,
    "--accordion__background-color--hover": theme.sonarColors.sonarGrey
  });
}

export function degradedStyle(theme: Theme) {
  return css({
    "--accordion__background-color": theme.sonarColors.sonarOrange,
    "--accordion__background-color--hover": theme.sonarColors.sonarOrange
  });
}

export function atRiskStyle(theme: Theme) {
  return css({
    "--accordion__background-color": theme.sonarColors.sonarGold,
    "--accordion__background-color--hover": theme.sonarColors.sonarGold
  });
}
