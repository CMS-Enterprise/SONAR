import { css, CSSObject, SerializedStyles, Theme } from '@emotion/react';
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

      whiteSpace: "pre-line",
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
    },
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

export const StatusHistoryDatePickerContainerStyle: CSSObject = {
  padding: "0px 5px 0px 5px",
  "& input": {
    margin: "0px 10px 0px 10px"
  }
};

export const StatusHistoryTimeRangeContainerStyle: CSSObject = {
  paddingLeft: "10px",
  margin: "10px 0px 10px 0px"
}

export const StatusHistoryTimeRangeOptionStyle: CSSObject = {
  paddingLeft: "10px",
  margin: "0px 10px 0px 20px",
  "& label": {
    marginTop: 0
  },
  "& .ds-c-field": {
    padding: "1px 25px 1px 10px",
    fontSize: "0.9rem"
  }
};

export const StatusHistoryQuickRangeTextFieldStyle :
  (theme: Theme) => SerializedStyles = theme => css ({
  display: 'inline-block',
  margin: "0px 10px 0px 10px",
  "& .ds-c-field": {
    padding: "0px 10px 0px 10px",
    fontSize: "0.9rem",
    backgroundColor: theme.backgroundColor,
    borderColor: theme.foregroundColor
  },
  "& .ds-c-label": {
    margin: 0
  }
});

export const StatusHistoryButtonStyle: CSSObject = {
  margin: "0px 2px 0px 2px"
}
