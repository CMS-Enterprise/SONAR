import React, { useEffect } from 'react';
import Chart from 'react-apexcharts';
import { ApexOptions } from "apexcharts";
import { Table, TableBody, TableCaption, TableCell, TableHead, TableRow } from '@cmsgov/design-system';

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
              <TableCell stackedTitle="Document title">
                {new Date(data[TIMESTAMP_DATA]).toTimeString()}
              </TableCell>
              <TableCell stackedTitle="Description">
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
