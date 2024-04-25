import { TableCell, TableRow } from '@cmsgov/design-system';
import React from 'react';
import { ActiveScheduledMaintenanceView } from '../../api/data-contracts';
import cronstrue from 'cronstrue';

const ScheduledMaintenanceTableItem: React.FC<{
  maintenance: ActiveScheduledMaintenanceView
}> =
  ({ maintenance }) => {

    let entityName = maintenance.environment!;
    if (maintenance.tenant) {
      entityName += `/${maintenance.tenant}`;
    }
    if (maintenance.service) {
      entityName += `/${maintenance.service}`;
    }

    return (
      <TableRow>
        <TableCell>
          {maintenance.scope}
        </TableCell>
        <TableCell>
          {entityName}
        </TableCell>
        <TableCell>
          {cronstrue.toString(maintenance.scheduleExpression)}
        </TableCell>
        <TableCell>
          {maintenance.duration + " minutes"}
        </TableCell>
        <TableCell>
          {maintenance.timeZone}
        </TableCell>
      </TableRow>
    );
  }

export default ScheduledMaintenanceTableItem;
