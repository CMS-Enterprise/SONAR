import { css, Theme } from '@emotion/react';
import { HealthStatus } from '../../api/data-contracts';
import { getStatusColors } from '../../helpers/StyleHelper';

export function getEnvironmentStatusStyle(status: HealthStatus | undefined, theme: Theme) {
  const statusColor = getStatusColors(theme, status);
  return css({
    "--accordion__background-color": statusColor,
    "--accordion__background-color--hover": statusColor
  });
}

export const EnvironmentItemContainerStyle = css({
  marginTop: 10,
  marginBottom: 10
})
