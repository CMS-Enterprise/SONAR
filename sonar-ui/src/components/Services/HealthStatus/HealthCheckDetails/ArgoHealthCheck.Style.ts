import { css, Theme } from '@emotion/react';
import { HealthStatus } from '../../../../api/data-contracts';
import { getStatusColors } from '../../../../helpers/StyleHelper';

export function getArgoStatusIndicatorIconStyle(theme: Theme, status: HealthStatus) {
  return css({
    fill: getStatusColors(theme, status),
    fontSize: 15
  });
}
