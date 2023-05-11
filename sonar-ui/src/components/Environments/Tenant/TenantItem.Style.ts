import { css, Theme } from '@emotion/react';
import { HealthStatus } from '../../../api/data-contracts';
import { getStatusColors } from '../../../helpers/StyleHelper';

export function getIconStyle(status: HealthStatus | undefined, theme: Theme) {
  return css({
    color: getStatusColors(theme, status)
  })
}

export const TenantItemStyle = css({
  display: "inline-block",
  width: "100%",
  whiteSpace: "nowrap",
  overflow: "hidden",
  textOverflow: "ellipsis"
});

export const TenantItemSpanStyle = css({
  verticalAlign:'middle',
  paddingLeft:'2px'
})

