import { TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import ApiKeyTableItem from './ApiKeyTableItem';
import { getEmptyTableMessageStyle } from './ApiKeyTable.Style';
import ThemedTable from 'components/Common/ThemedTable';

const ApiKeyTable: React.FC<{
  apiKeys: ApiKeyConfiguration[]
}> =
  ({ apiKeys }) => {
    const theme = useTheme();
    return (
      <div className='ds-l-row'>
        <div className='ds-l-col--12'>
          {apiKeys.length > 0 ? (
            <ThemedTable>
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
                  <TableCell>
                    Creation
                  </TableCell>
                  <TableCell>
                    Last Used
                  </TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {apiKeys.map(apiKey => (
                  <ApiKeyTableItem key={apiKey.id} apiKey={apiKey} />
                ))}
              </TableBody>
            </ThemedTable>
          ) : (
            <div className='ds-u-margin-top--1' css={getEmptyTableMessageStyle(theme)}>
              There are no API Keys associated with your account
            </div>
          )}
        </div>
      </div>
    );
  }
export default ApiKeyTable;
