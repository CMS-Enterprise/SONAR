import React, { useState } from 'react';
import { ArrowIcon, TableCell, TableRow } from '@cmsgov/design-system';
import { ErrorReportDetails } from '../../api/data-contracts';
import ExpandableRowButton from '../Common/ExpandableRowButton';
import {
  buttonCellStyle,
  timestampCellStyle,
  levelCellStyle,
  typeCellStyle,
  errorMessageCellStyle
} from './ErrorReportsCell.Style';
import ErroReportsTableSubRow from './ErrorReportsTableSubRow';
import dayjs from "dayjs";

const ErrorReportTableRow: React.FC<{
  errorReport: ErrorReportDetails
}> = ({ errorReport }) => {
  const [open, setOpen] = useState<boolean>(false);
  const handleSubRowToggle = () => {
    setOpen(!open);
  }

  return (
    <>
      <TableRow>
        <TableCell css={buttonCellStyle} align='center'>
          <ExpandableRowButton onClick={handleSubRowToggle}>
            {open ? <ArrowIcon direction='down' /> : <ArrowIcon direction='right' />}
          </ExpandableRowButton>
        </TableCell>
        <TableCell css={timestampCellStyle}>
          { dayjs(errorReport.timestamp).format("MM/DD/YYYY HH:mm:ss")}
        </TableCell>
        <TableCell css={levelCellStyle}>
          {errorReport.level}
        </TableCell>
        <TableCell css={typeCellStyle}>
          {errorReport.type}
        </TableCell>
        <TableCell css={errorMessageCellStyle}>
          {errorReport.message}
        </TableCell>
      </TableRow>
      {open && <ErroReportsTableSubRow errorReport={errorReport}/>}
    </>
  );
}

export default ErrorReportTableRow;
