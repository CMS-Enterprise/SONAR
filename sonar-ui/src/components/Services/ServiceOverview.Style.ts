import { css, CSSObject, Theme } from '@emotion/react';

export function getServiceOverviewStyle(theme: Theme) {
  return css({
    backgroundColor: theme.foregroundColor,
    boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px",
    borderRadius: 40,
    padding: 20
  });
}

export const ServiceOverviewHeaderStyle: CSSObject = {
  padding: 10,
  fontSize: 18,
  fontWeight: 500
}

export const CollapsibleHeaderStyle: CSSObject = {
  "summary": {
    padding: 10,
    fontSize: 18,
    fontWeight: 500
  }
}

export const ServiceOverviewContentStyle: CSSObject = {
  paddingLeft: 10,
  display: "flex",
  alignItems: "center"
}
export function getSubContainerStyle(theme: Theme) {
  return css({
    height: "40px",
    lineHeight: "40px",
    borderRadius: 20,
    paddingLeft: 10,
    "&:hover": {
      backgroundColor: theme.highlightColor
    }
  });
}

export function getSubsectionContainerStyle(theme: Theme) {
  return css({
    margin: "0px 24px 0px 24px",
    boxSizing: "border-box",
    borderLeft: "2px solid " + theme.textColor,
    paddingLeft: "24px",
    "&:hover": {
      borderLeft: "4px solid " + theme.accentColor,
      // Compensate for the increased border width
      paddingLeft: "22px"
    }
  });
}
