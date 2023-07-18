import { Table, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import ApiKeyTableItem from './ApiKeyTableItem';
import { getEmptyTableMessageStyle, getTableContainerStyle, getTableStyle } from './ApiKeyTable.Style';

const ApiKeyTable: React.FC<{
  apiKeys: ApiKeyConfiguration[]
}> =
  ({ apiKeys }) => {
    const theme = useTheme();
    return (
      <div className="ds-l-row">
        <div className="ds-l-col--11 ds-u-margin-left--auto ds-u-margin-right--auto ds-u-margin-top--3" css={getTableContainerStyle(theme)}>
          {apiKeys.length > 0 ? (
            <Table borderless css={getTableStyle(theme)}>
              <TableHead>
                <TableRow>
                  <TableCell>
                    ID
                  </TableCell>
                  <TableCell>
                    Role
                  </TableCell>
                  <TableCell>
                    Environment
                  </TableCell>
                  <TableCell>
                    Tenant
                  </TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {apiKeys.map(apiKey => (
                  <ApiKeyTableItem key={apiKey.id} apiKey={apiKey} />
                ))}
              </TableBody>
            </Table>
          ) : (
            <div css={getEmptyTableMessageStyle(theme)}>
              There are no API Keys associated with your account
            </div>
          )}
        </div>
      </div>
    );
  }
export default ApiKeyTable;
