import { TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import React from 'react';
import { ErrorReportDetails } from 'api/data-contracts';
import ErrorReportTableRow from './ErrorReportTableRow';
import ThemedTable from 'components/Common/ThemedTable';
import {
  buttonCellStyle,
  timestampCellStyle,
  levelCellStyle,
  typeCellStyle,
  errorMessageHeaderCellStyle
} from './ErrorReportsCell.Style';

const ErrorReportsTable: React.FC<{
  errorReports: ErrorReportDetails[]
}> = ({ errorReports}) => {
  return (
    <div className='ds-l-row'>
      <div className='ds-l-col--12'>
        <ThemedTable>
          <TableHead>
            <TableRow>
              <TableCell css={buttonCellStyle}/>
              <TableCell css={timestampCellStyle}>
                Timestamp
              </TableCell>
              <TableCell css={levelCellStyle}>
                Level
              </TableCell>
              <TableCell css={typeCellStyle}>
                Type
              </TableCell>
              <TableCell css={errorMessageHeaderCellStyle}>
                Message
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {errorReports.map((report, i) => (
              <ErrorReportTableRow key={i} errorReport={report} />
            ))}
          </TableBody>
        </ThemedTable>
      </div>
    </div>
  );
};

export default ErrorReportsTable;
