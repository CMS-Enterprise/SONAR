import { Button } from '@cmsgov/design-system';
import React, { useEffect, useState } from 'react';
import { getHealthStatusClass } from '../../helpers/ServiceHierarchyHelper';

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
        isAlternate
        variation="solid"
        onClick={handleSelect}
        size="small"
        className={selectedTileId === id ? "selected" : getHealthStatusClass(statusTimestampTuple.status)}
        style={{ borderRadius: 9 }}
      >
        {statusTimestampTuple.timestamp}<br /> {statusTimestampTuple.status.toString()}
      </Button>
    </span>

  )
}

export default StatusHistoryTile;
