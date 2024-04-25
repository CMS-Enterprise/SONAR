import { TableCell, TableRow } from '@cmsgov/design-system';
import dayjs from 'dayjs';
import React, { useState } from 'react';
import { ActiveAdHocMaintenanceView } from '../../api/data-contracts';
import DeleteIcon from '../Icons/DeleteIcon';
import GhostActionButton from 'components/Common/GhostActionButton';
import RemoveAdhocMaintenanceModal from './RemoveAdhocMaintenanceModal';

const AdhocMaintenanceTableItem: React.FC<{
  maintenance: ActiveAdHocMaintenanceView
}> =
  ({ maintenance }) => {
    const [open, setOpen] = useState<boolean>(false);
    const handleModalToggle = () => {
      setOpen(!open);
    }

    let entityName = maintenance.environment!;
    if (maintenance.tenant) {
      entityName += `/${maintenance.tenant}`;
    }
    if (maintenance.service) {
      entityName += `/${maintenance.service}`;
    }

    return <TableRow>
      <TableCell>
        {maintenance.scope}
      </TableCell>
      <TableCell>
        {entityName}
      </TableCell>
      <TableCell>
        {maintenance.appliedByUserName}
      </TableCell>
      <TableCell>
        {dayjs(maintenance.startTime).format("MM/DD/YYYY HH:mm:ss")}
      </TableCell>
      <TableCell>
        {dayjs(maintenance.endTime).format("MM/DD/YYYY HH:mm:ss")}
      </TableCell>
      <TableCell>
        <GhostActionButton onClick={handleModalToggle}>
          <DeleteIcon /> Remove Maintenance Window
        </GhostActionButton>
        { open ?
          <RemoveAdhocMaintenanceModal
            handleModalToggle={handleModalToggle}
            entityName={entityName}
            maintenance={maintenance}
          /> : null}
      </TableCell>
    </TableRow>
  }

export default AdhocMaintenanceTableItem;
