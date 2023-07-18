import { Spinner } from '@cmsgov/design-system';
import React, { useMemo, useState } from 'react';
import { ApiKeyConfiguration } from '../api/data-contracts';
import { parentContainerStyle } from '../App.Style';
import ApiKeyHeader from '../components/ApiKeys/ApiKeyHeader';
import ApiKeyTable from '../components/ApiKeys/ApiKeyTable';
import TablePagination from '../components/App/TablePagination';
import { createSonarClient } from '../helpers/ApiHelper';
import useAuthenticatedQuery from '../helpers/UseAuthenicatedQuery';

const PAGE_LIMIT = 2;

const ApiKeys = () => {
  const sonarClient = createSonarClient();
  const [currentPage, setCurrentPage] = useState(1);
  const { isLoading, data } = useAuthenticatedQuery<ApiKeyConfiguration[], Error>(
    ["apiKeys"],
    (_, token) => {
      return sonarClient
        .v2KeysList(
          {
            headers: {
              'Authorization': `Bearer ${token}`
            }
          })
        .then((res) => res.data)
    },
    {enabled: true}
  );
  const totalPages = Math.ceil((data ? data.length : 0) / PAGE_LIMIT);

  const currentTableData = useMemo(() => {
    if (!data) {
      return [];
    }
    const firstPageIndex = (currentPage - 1) * PAGE_LIMIT;
    const lastPageIndex = firstPageIndex + PAGE_LIMIT;
    return data?.slice(firstPageIndex, lastPageIndex);
  }, [currentPage, data]);

  return (
    <section className="ds-l-container" css={parentContainerStyle()}>
      <ApiKeyHeader />
      {isLoading ? (<Spinner />) : (
        <>
          <ApiKeyTable apiKeys={currentTableData}/>
          {currentTableData?.length > PAGE_LIMIT ? (
            <TablePagination
              currentPage={currentPage}
              totalPages={totalPages}
              itemsPerPage={PAGE_LIMIT}
              handleSelectPage={setCurrentPage}
            />
          ) : null}

        </>
      )}
    </section>
  )
}

export default ApiKeys;
