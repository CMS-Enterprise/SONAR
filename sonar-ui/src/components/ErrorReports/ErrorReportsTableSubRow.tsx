import React from 'react';
import { ErrorReportDetails } from 'api/data-contracts';
import ErrorReportTableSubRowCell from './ErrorReportTableSubRowCell';

const ErrorReportsTableSubRow: React.FC<{
  errorReport: ErrorReportDetails
}> = ({ errorReport}) => {
  let configInfo: string;
  let isParsed = false;
  try{
    configInfo = errorReport.configuration ? JSON.stringify(JSON.parse(errorReport.configuration), null, 2) : "";
    isParsed = true;
  } catch (e) {
    configInfo = errorReport.configuration ? errorReport.configuration : "";
  }
  return (
    <>
      <ErrorReportTableSubRowCell
        cellDescription='Full Error Message:'
        cellDetail={errorReport.message}
      />
      {errorReport.configuration &&
        <ErrorReportTableSubRowCell
          cellDescription='Configuration:'
          cellDetail={isParsed ? <pre>{configInfo}</pre> : configInfo}
        />
      }
      {errorReport.stackTrace &&
        <ErrorReportTableSubRowCell
          cellDescription='Error Stack Trace:'
          cellDetail={errorReport.stackTrace}
        />
      }
    </>
  );
}

export default ErrorReportsTableSubRow;
