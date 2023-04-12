import React from 'react';
import { HealthStatus } from '../../api/data-contracts';
import { HeadingContainer, StatusHistoryContainer } from '../../styles';
import StatusHistoryTile from './StatusHistoryTile';

const statusHistoryItems = [
  { id: 1, timestamp: "8:00", status: HealthStatus.Online },
  { id: 2, timestamp: "8:10", status: HealthStatus.Degraded },
  { id: 3, timestamp: "8:20", status: HealthStatus.Online },
  { id: 4, timestamp: "8:30", status: HealthStatus.AtRisk },
  { id: 5, timestamp: "8:40", status: HealthStatus.Online },
  { id: 6, timestamp: "8:50", status: HealthStatus.Degraded },
  { id: 7, timestamp: "9:00", status: HealthStatus.Online },
  { id: 8, timestamp: "9:10", status: HealthStatus.Offline },
  { id: 9, timestamp: "9:20", status: HealthStatus.Online },
  { id: 10, timestamp: "9:30", status: HealthStatus.Unknown }
]

const StatusHistoryModule: React.FC<{
  addTimestamp: (tileData: any, tileId: string) => void,
  closeDrawer: () => void,
  selectedTileId: string,
  rootServiceName: string
}> = ({ addTimestamp, closeDrawer, selectedTileId, rootServiceName }) => {
  return (
    <>
      <div style={HeadingContainer}>
        Status History:
      </div>
      <div style={StatusHistoryContainer}>
        {statusHistoryItems.map((item, index) => (
          <StatusHistoryTile
            key={item.timestamp}
            id={`${rootServiceName}-${index}`}
            statusTimestampTuple={item}
            addTimestamp={addTimestamp}
            closeDrawer={closeDrawer}
            selectedTileId={selectedTileId}
          />
        ))}
      </div>
    </>

  )
}

export default StatusHistoryModule;
