import { Button } from '@cmsgov/design-system';
import React from 'react';
import { DateTimeHealthStatusValueTuple, HealthStatus, ServiceHierarchyHealth } from '../../api/data-contracts';
import { getHealthStatusClass } from '../../helpers/ServiceHierarchyHelper';
import { renderStatusIcon } from '../../helpers/StatusHistoryHelper';

const StatusHistoryTile: React.FC<{
  id: string,
  statusTimestampTuple: DateTimeHealthStatusValueTuple,
  addTimestamp: (tupleData: DateTimeHealthStatusValueTuple, tileId: string, serviceData: ServiceHierarchyHealth) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  rootService: ServiceHierarchyHealth
}> = ({ id, statusTimestampTuple, addTimestamp, closeDrawer, selectedTileId, rootService }) => {
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
    <span style={{ margin: 2 }}>
      <Button
        variation="solid"
        onClick={handleSelect}
        size="small"
        className={getHealthStatusClass(status) + '-tile' + (selectedTileId === id ? " selected" : "")}
        style={{ borderRadius: 9 }}
      >
        {renderStatusIcon(status)}
      </Button>
    </span>

  )
}

export default StatusHistoryTile;
