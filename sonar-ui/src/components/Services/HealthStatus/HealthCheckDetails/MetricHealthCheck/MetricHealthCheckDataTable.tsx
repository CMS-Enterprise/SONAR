import React from 'react';
import { Table, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { TextAlignCenter, getChartsTablePropertiesStyle } from './MetricHealthCheck.Style';
import { DynamicTextFontStyle } from 'App.Style';

const MetricHealthCheckDataTable: React.FC<{
  healthCheckName: string
  timeSeriesData: number[][],
  isResponseTimeCondition: boolean
}> = ({ healthCheckName, timeSeriesData, isResponseTimeCondition }) => {
  const TIMESTAMP_DATA = 0;
  const HEALTHSTATUS_DATA = 1;

  return (
    <Table css={getChartsTablePropertiesStyle}>
      <TableHead>
        <TableRow >
          <TableCell>Timestamp</TableCell>
          <TableCell css={TextAlignCenter}>{isResponseTimeCondition ? "Response Time" : "HealthStatus"}</TableCell>
        </TableRow>
      </TableHead>
      <TableBody css={DynamicTextFontStyle}>
        {timeSeriesData?.map((data, index:number) =>
          <TableRow key={healthCheckName+'-row-'+index}>
            <TableCell>{new Date(data[TIMESTAMP_DATA]).toISOString()}</TableCell>
            <TableCell css={TextAlignCenter}>{data[HEALTHSTATUS_DATA]}{isResponseTimeCondition ? "s" : null}</TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
};

export default MetricHealthCheckDataTable;
