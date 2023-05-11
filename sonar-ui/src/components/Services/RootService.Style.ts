import { css, Theme } from '@emotion/react';
import { HealthStatus } from '../../api/data-contracts';
import { getStatusColors } from '../../helpers/StyleHelper';

export function getRootServiceStyle(theme: Theme, status: HealthStatus | undefined) {
  return css({
    margin: 10,
    borderStyle: "solid",
    padding: 5,
    borderColor: getStatusColors(theme, status)
  });
}

export function getContainerStyle() {
  return css({
    padding: 5
  });
}

