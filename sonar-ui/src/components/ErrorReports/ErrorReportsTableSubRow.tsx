import React from 'react';
import { ErrorReportDetails } from 'api/data-contracts';
import ErrorReportTableSubRowCell from './ErrorReportTableSubRowCell';

const ErrorReportsTableSubRow: React.FC<{
  errorReport: ErrorReportDetails
}> = ({ errorReport}) => {
  return (
    <>
      <ErrorReportTableSubRowCell
        cellDescription='Full Error Message:'
        cellDetail={errorReport.message}
      />
      { errorReport.configuration &&
        <ErrorReportTableSubRowCell
          cellDescription='Configuration:'
          cellDetail={errorReport.configuration}
        />
      }
      { errorReport.stackTrace &&
        <ErrorReportTableSubRowCell
          cellDescription='Error Stack Trace:'
          cellDetail={errorReport.stackTrace}
        />
      }
    </>
  );
}

export default ErrorReportsTableSubRow;
