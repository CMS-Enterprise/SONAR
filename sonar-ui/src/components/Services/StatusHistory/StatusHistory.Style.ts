import { css, CSSObject, Theme } from '@emotion/react';
import { HealthStatus } from 'api/data-contracts';
import { getStatusColors } from 'helpers/StyleHelper';

export const StatusHistoryChecklistStyle: CSSObject = {
  marginTop: 10,
  marginBottom: 10
};

export const TileSpanStyle: CSSObject = {
  margin: 2
};

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
    },
    ...selected && { borderColor: "#bd13b8", borderWidth: 3}
  })
}

export const StatusHistoryTileContainerStyle: CSSObject = {
  padding: "0px 10px 0px 10px"
};
