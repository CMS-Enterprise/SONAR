import React from 'react';
import { Table, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';

const ChartsTable: React.FC<{
  timeSeriesData: number[][]
}> = ({ timeSeriesData }) => {
  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  return (
    <div>
      <Table scrollable>
        <TableHead>
          <TableRow>
            <TableCell>
              Timestamp
            </TableCell>
            <TableCell>
              HealthStatus
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {timeSeriesData.map(data =>
            <TableRow>
              <TableCell>
                {new Date(data[TIMESTAMP_DATA]).toTimeString()}
              </TableCell>
              <TableCell>
                {data[HEALTHSTATUS_DATA]}
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
};

export default ChartsTable;