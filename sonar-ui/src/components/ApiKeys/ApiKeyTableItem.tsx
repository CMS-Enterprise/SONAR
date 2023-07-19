import { Button, TableCell, TableRow } from '@cmsgov/design-system';
import React from 'react';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import * as styles from '../App/Header.Style';
import DeleteIcon from '../Icons/DeleteIcon';

const ApiKeyTableItem: React.FC<{
  apiKey: ApiKeyConfiguration
}> =
  ({ apiKey}) => {

  return (
    <TableRow>
      <TableCell>
        {apiKey.id}
      </TableCell>
      <TableCell>
        {apiKey.apiKeyType}
      </TableCell>
      <TableCell>
        {apiKey.environment ? apiKey.environment : "All Environments"}
      </TableCell>
      <TableCell>
        {apiKey.tenant ? apiKey.tenant : "All Tenants"}
      </TableCell>
      <TableCell>
        <Button
          css={[styles.ButtonStyles, styles.IconButtonStyle]}
          size={'small'}
        >
          <DeleteIcon /> Delete
        </Button>
      </TableCell>
    </TableRow>
  )
}

export default ApiKeyTableItem;
