import React from 'react';
import { Table, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { chartsTableProperties } from './HealthCheckListItem.Style';


const HealthCheckListItemTimeSeriesChart: React.FC<{
  healthCheckName: string
  timeSeriesData: number[][]
}> = ({ healthCheckName, timeSeriesData }) => {
  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  return (
    <Table style={chartsTableProperties}>
      <TableHead>
        <TableRow >
          <TableCell>Timestamp</TableCell>
          <TableCell>HealthStatus</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {timeSeriesData?.map((data, index:number) =>
          <TableRow key={healthCheckName+'-row-'+index}>
            <TableCell>{new Date(data[TIMESTAMP_DATA]).toISOString()}</TableCell>
            <TableCell>{data[HEALTHSTATUS_DATA]}</TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
};

export default HealthCheckListItemTimeSeriesChart;
