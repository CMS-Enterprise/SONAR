import { Button } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import {
  DateTimeHealthStatusValueTuple,
  HealthStatus,
  ServiceHierarchyHealth
} from '../../../api/data-contracts';
import {
  renderStatusIcon,
  convertUtcTimestampToLocal
} from '../../../helpers/StatusHistoryHelper';
import { getStatusHistoryTileStyle, getTileSpanStyle } from './StatusHistory.Style';

const StatusHistoryTile: React.FC<{
  id: string,
  statusTimestampTuple: DateTimeHealthStatusValueTuple,
  addTimestamp: (
    tupleData: DateTimeHealthStatusValueTuple,
    tileId: string,
    serviceData: ServiceHierarchyHealth
  ) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  serviceHealth: ServiceHierarchyHealth,
  showDate: boolean,
  envName: string,
  tenantName: string,
  servicePath: string,
  rangeInSeconds: number
}> = ({
  id,
  statusTimestampTuple,
  addTimestamp,
  closeDrawer,
  selectedTileId,
  serviceHealth,
  showDate,
  envName,
  tenantName,
  servicePath,
  rangeInSeconds
}) => {
  const theme = useTheme();

  const handleSelect = () => {
    if (selectedTileId !== id) {
      addTimestamp(statusTimestampTuple, id, serviceHealth);
    } else {
      // close drawer
      closeDrawer();
    }
  }

  const status: HealthStatus = HealthStatus[statusTimestampTuple[1] as keyof typeof HealthStatus]
  const convertedTimestamp = convertUtcTimestampToLocal(statusTimestampTuple[0], showDate);
  const tooltipText = `${convertedTimestamp}\n${statusTimestampTuple[1]}`;

  return (
    <span css={getTileSpanStyle(theme)} data-tooltip={tooltipText}>
      <Button
        aria-label={tooltipText}
        variation="solid"
        onClick={handleSelect}
        size="small"
        css={getStatusHistoryTileStyle(theme, status, selectedTileId === id)}
      >
        {renderStatusIcon(status)}
      </Button>
    </span>
  )
}

export default StatusHistoryTile;
