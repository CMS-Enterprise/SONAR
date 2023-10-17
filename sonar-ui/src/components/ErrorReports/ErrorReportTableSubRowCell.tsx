import { TableCell, TableRow } from '@cmsgov/design-system';
import React from 'react';
import { buttonCellStyle } from './ErrorReportsCell.Style';

const ErrorReportTableSubRowCell: React.FC<{
  cellDescription: string,
  cellDetail: string
}> = ({ cellDescription, cellDetail }) => {
  return (
    <TableRow>
      <TableCell css={buttonCellStyle} />
      <TableCell>
        {cellDescription}
      </TableCell>
      <TableCell colSpan={3}>
        {cellDetail}
      </TableCell>
    </TableRow>
  );
}

export default ErrorReportTableSubRowCell;
