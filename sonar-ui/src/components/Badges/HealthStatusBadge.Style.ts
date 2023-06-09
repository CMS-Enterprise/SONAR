import { css, Theme } from '@emotion/react';
import { HealthStatus } from '../../api/data-contracts';
import { getStatusColors } from '../../helpers/StyleHelper';

export function getBadgeTextColor(status: HealthStatus | undefined) {
  let color;
  switch (status) {
    case HealthStatus.Online:
    case HealthStatus.Offline:
      color = '#FFF';
      break;
    case HealthStatus.Unknown:
    case HealthStatus.AtRisk:
    case HealthStatus.Degraded:
      color = '#262626';
      break;
    default:
      color = '#262626';
  }
  return color;
}

export function badgeStyle(theme: Theme, status: HealthStatus | undefined) {
  return css({
    color: getBadgeTextColor(status),
    textAlign: 'center',
    width: '70px',
    fontSize: "14px",
    "--badge__background-color": getStatusColors(theme, status)
  });
}

export function getBadgeSpanStyle(theme: Theme) {
  return css({
    verticalAlign:'middle',
    paddingLeft:'15px',
    color: theme.textColor,
    "&:hover": {
      color: theme.accentColor,
      cursor: 'pointer'
    }
  })
}

export function getBadgeSpanNoLinkStyle(theme: Theme) {
  return css({
    verticalAlign:'middle',
    paddingLeft:'15px',
    color: theme.textColor
  })
}
