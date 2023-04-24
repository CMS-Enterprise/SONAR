import { Button } from '@cmsgov/design-system';
import React, { useEffect, useState } from 'react';
import { HealthStatus } from '../../api/data-contracts';
import { getHealthStatusClass } from '../../helpers/ServiceHierarchyHelper';
import { renderStatusIcon } from '../../helpers/StatusHistoryHelper';

const StatusHistoryTile: React.FC<{
  id: string,
  statusTimestampTuple: any,
  addTimestamp: (tileData: any, tileId: string) => void,
  closeDrawer: () => void,
  selectedTileId: string
}> = ({ id, statusTimestampTuple, addTimestamp, closeDrawer, selectedTileId }) => {

  const handleSelect = () => {
    if (selectedTileId !== id) {
      addTimestamp(statusTimestampTuple, id)
    } else {
      // close drawer
      closeDrawer();
    }
  }



  return (
    <span style={{ margin: 2 }}>
      <Button
        variation="solid"
        onClick={handleSelect}
        size="small"
        className={getHealthStatusClass(statusTimestampTuple.status, true) + (selectedTileId === id ? " selected" : "")}
        style={{ borderRadius: 9 }}
      >
        {renderStatusIcon(statusTimestampTuple.status)}
      </Button>
    </span>

  )
}

export default StatusHistoryTile;
