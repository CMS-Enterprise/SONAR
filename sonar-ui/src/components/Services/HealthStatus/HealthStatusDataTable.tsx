import React from 'react';
import { Table, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { TextAlignCenter, getChartsTablePropertiesStyle } from './HealthStatus.Style';
import { DynamicTextFontStyle } from 'App.Style';

const HealthStatusDataTable: React.FC<{
  healthCheckName: string
  timeSeriesData: number[][]
}> = ({ healthCheckName, timeSeriesData }) => {
  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  return (
    <Table css={getChartsTablePropertiesStyle}>
      <TableHead>
        <TableRow >
          <TableCell>Timestamp</TableCell>
          <TableCell css={TextAlignCenter}>HealthStatus</TableCell>
        </TableRow>
      </TableHead>
      <TableBody css={DynamicTextFontStyle}>
        {timeSeriesData?.map((data, index:number) =>
          <TableRow key={healthCheckName+'-row-'+index}>
            <TableCell>{new Date(data[TIMESTAMP_DATA]).toISOString()}</TableCell>
            <TableCell css={TextAlignCenter}>{data[HEALTHSTATUS_DATA]}</TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
};

export default HealthStatusDataTable;
