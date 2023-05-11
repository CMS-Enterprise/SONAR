import { Theme } from '@emotion/react';
import { HealthStatus } from '../api/data-contracts';

export function getStatusColors (theme: Theme, status: HealthStatus | undefined) {
  let color;
  switch (status) {
    case HealthStatus.Online:
      color = theme.sonarColors.sonarGreen
      break;
    case HealthStatus.Offline:
      color = theme.sonarColors.sonarRed
      break;
    case HealthStatus.Unknown:
      color = theme.sonarColors.sonarGrey
      break;
    case HealthStatus.Degraded:
      color = theme.sonarColors.sonarOrange
      break;
    case HealthStatus.AtRisk:
      color = theme.sonarColors.sonarGold
      break;
    default:
      color = theme.sonarColors.sonarGrey
  }
  return color;
}
