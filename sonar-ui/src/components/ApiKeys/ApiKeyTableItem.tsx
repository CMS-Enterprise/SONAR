import { TableCell, TableRow } from '@cmsgov/design-system';
import React, { useState } from 'react';
import { ApiKeyConfiguration } from '../../api/data-contracts';
import DeleteIcon from '../Icons/DeleteIcon';
import DeleteKeyModal from './DeleteKeyModal';
import GhostActionButton from 'components/Common/GhostActionButton';

const ApiKeyTableItem: React.FC<{
  apiKey: ApiKeyConfiguration
}> =
  ({ apiKey}) => {
    const [open, setOpen] = useState<boolean>(false);
    const handleModalToggle = () => {
      setOpen(!open);
    }


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
        <GhostActionButton onClick={handleModalToggle}>
          <DeleteIcon /> Delete
        </GhostActionButton>
        { open ?
          <DeleteKeyModal
          handleModalToggle={handleModalToggle}
          apiKey={apiKey}
        /> : null}
      </TableCell>
    </TableRow>
  )
}

export default ApiKeyTableItem;
