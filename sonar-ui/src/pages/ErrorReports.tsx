import { Spinner } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import { parentContainerStyle } from 'App.Style';
import {getEmptyTableMessageStyle} from '../components/Common/ThemedTableStyle';
import Breadcrumbs from 'components/Services/Breadcrumbs/Breadcrumbs';
import { useListErrorReports } from 'components/ErrorReports/ErrorReports.Hooks';
import ErrorReportsTable from 'components/ErrorReports/ErrorReportsTable';

const ErrorReports = () => {
  const theme = useTheme();

  const params = useParams();
  const environmentName = params.environment as string;

  const [searchParams] = useSearchParams();
  const query = {
    start: searchParams.get('start') ?? undefined,
    end: searchParams.get('end') ?? undefined
  };

  const { isLoading, data } = useListErrorReports(environmentName, query);
  const errorReports = data ?? [];

  return (
    <section className="ds-l-container" css={parentContainerStyle}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%" }}>
        <Breadcrumbs />
      </div>
      { isLoading ? (<Spinner />) : (
        errorReports.length > 0 ?
          <ErrorReportsTable errorReports={errorReports} /> :
          <div className='ds-u-margin-top--1' css={getEmptyTableMessageStyle(theme)}>
            There are 0 associated error reports.
          </div>
      )}
    </section>
  )
}

export default ErrorReports;
