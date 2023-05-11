import { Button } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { DateTimeHealthStatusValueTuple, HealthStatus, ServiceHierarchyHealth } from '../../../api/data-contracts';
import { renderStatusIcon } from '../../../helpers/StatusHistoryHelper';
import { getStatusHistoryTileStyle, TileSpanStyle } from './StatusHistory.Style';

const StatusHistoryTile: React.FC<{
  id: string,
  statusTimestampTuple: DateTimeHealthStatusValueTuple,
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  rootService: ServiceHierarchyHealth
}> = ({ id, statusTimestampTuple, addTimestamp, closeDrawer, selectedTileId, rootService }) => {
  const theme = useTheme();
  const handleSelect = () => {
    if (selectedTileId !== id) {
      addTimestamp(statusTimestampTuple, id, rootService);
    } else {
      // close drawer
      closeDrawer();
    }
  }

  const status: HealthStatus = HealthStatus[statusTimestampTuple[1] as keyof typeof HealthStatus]

  return (
    <span css={TileSpanStyle}>
      <Button
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
