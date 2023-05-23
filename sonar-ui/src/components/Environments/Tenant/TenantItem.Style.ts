import { css, Theme } from '@emotion/react';
import { HealthStatus } from '../../../api/data-contracts';
import { getStatusColors } from '../../../helpers/StyleHelper';

export function getIconStyle(status: HealthStatus | undefined, theme: Theme) {
  return css({
    color: getStatusColors(theme, status)
  })
}

export function getTenantItemStyle(theme: Theme) {
  return css({
    display: "inline-block",
    width: "100%",
    whiteSpace: "nowrap",
    overflow: "hidden",
    textOverflow: "ellipsis",
    padding: "5px 5px 10px 10px",
    ":hover": {
      backgroundColor: theme.highlightColor,
      borderRadius: 15
    }
  });
}

export function getTenantItemSpanStyle(theme: Theme) {
  return css({
    verticalAlign:'middle',
    paddingLeft:'15px',
    color: theme.textColor
  })
}

