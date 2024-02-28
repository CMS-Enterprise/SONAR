import { TableCell, TableRow } from '@cmsgov/design-system';
import React from 'react';
import {
  buttonCellStyle,
  errorInfoCellStyle,
  subRowCellDetailStyle
} from './ErrorReportsCell.Style';

const ErrorReportTableSubRowCell: React.FC<{
  cellDescription: string,
  cellDetail: string | object;
}> = ({ cellDescription, cellDetail }) => {
  return (
    <TableRow>
      <TableCell css={buttonCellStyle} />
      <TableCell>
        {cellDescription}
      </TableCell>
      <TableCell colSpan={3} css={subRowCellDetailStyle}>
        <div css={errorInfoCellStyle}>
          {cellDetail}
        </div>
      </TableCell>
    </TableRow>
  );
}

export default ErrorReportTableSubRowCell;
