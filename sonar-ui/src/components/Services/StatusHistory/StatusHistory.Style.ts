import { css, CSSObject, Theme } from '@emotion/react';
import { HealthStatus } from 'api/data-contracts';
import { getStatusColors } from 'helpers/StyleHelper';

export const StatusHistoryChecklistStyle: CSSObject = {
  marginTop: 14,
  marginBottom: 10
};

export function getTileSpanStyle(theme: Theme) {
  return css({
    margin: 2,
    position: "relative",
    "&:before,:after": {
      "--scale": 0,
      "--arrow-size": "10px",

      position: "absolute",
      top: "-.5rem",
      left: "50%",
      transform: "translateX(-50%) translateY(var(--translate-y, 0)) scale(var(--scale))",
      transition: "150ms transform",
      transformOrigin: "bottom center"
    },
    "&:before": {
      "--translate-y": "calc(-100% - var(--arrow-size))",

      content: "attr(data-tooltip)",
      color: theme.foregroundColor,
      padding: "0.5rem",
      borderRadius: 5,
      textAlign: "center",
      width: "max-content",
      background: theme.accentColor
    },
    "&:hover:before,:hover:after": {
      "--scale": 1
    },
    "&:after": {
      "--translate-y": "calc(-1 * var(--arrow-size))",
      content: '""',
      border: "var(--arrow-size) solid transparent",
      borderTopColor: theme.accentColor,
      transformOrigin: "top center"
    }
  })
}

export function getStatusHistoryTileStyle(theme: Theme, status: HealthStatus | undefined, selected: boolean) {
  const textColor = (status === HealthStatus.AtRisk || status === HealthStatus.Degraded) ?
    "black" : "white";
  const statusColor = getStatusColors(theme, status);
  return css({
    backgroundColor: statusColor,
    color: textColor,
    borderRadius: 9,
    "&:hover": {
      color: textColor,
      "--button-solid__background-color--hover": statusColor,
      boxShadow: `0 0 0 3px ${theme.foregroundColor},0 0 4px 6px ${theme.accentColor}`
    },
    ...selected && { borderColor: theme.accentColor, borderWidth: 3},
    "--color-focus-dark": theme.accentColor,
    "--borderColor--h": theme.accentColor,
    "&:active": {
      backgroundColor: theme.accentColor,
      borderColor: theme.foregroundColor,
      color: theme.foregroundColor
    }
  })
}

export const StatusHistoryTileContainerStyle: CSSObject = {
  padding: "0px 10px 0px 10px"
};

export const StatusHistoryDrawerSubsectionStyle: CSSObject = {
  padding: "10px 15px 10px 15px",
  fontSize: 16
};
