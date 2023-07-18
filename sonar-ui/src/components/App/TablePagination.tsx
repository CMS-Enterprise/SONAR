import { Pagination } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { getTablePaginationStyle } from './TablePagination.Style';

const TablePagination: React.FC<{
  currentPage: number,
  totalPages: number,
  itemsPerPage: number,
  handleSelectPage: (value: number) => void
}> =
  ({ currentPage, totalPages, handleSelectPage}) => {
  const theme = useTheme();
  return (
    <div className="ds-l-row">
      <div className="ds-l-col--11 ds-u-margin-left--auto ds-u-margin-right--auto ds-u-margin-top--3" css={getTablePaginationStyle(theme)}>
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={(_, page: number) =>
            handleSelectPage(page)}
          renderHref={() => {return ""}}
        />
      </div>
    </div>
      )
}

export default TablePagination;
