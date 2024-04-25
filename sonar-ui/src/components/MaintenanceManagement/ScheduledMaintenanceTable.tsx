import { Spinner, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import {getEmptyTableMessageStyle} from '../Common/ThemedTableStyle';
import ThemedTable from '../Common/ThemedTable';
import { useGetActiveScheduledMaintenances } from './Maintenance.Hooks';
import ScheduledMaintenanceTableItem from './ScheduledMaintenanceTableItem';

const ScheduledMaintenanceTable = () => {
  const theme = useTheme();
  const data = useGetActiveScheduledMaintenances();
  const isLoading = data.some(e => e.isLoading);
  const mergedData = data.flatMap(e => e.data ?? []);
  return (
    <>
      <div className='ds-l-row'>
        <div className='ds-l-col--12'>
          {isLoading ?? <Spinner />}
          {mergedData.length > 0 ? (
            <ThemedTable>
              <TableHead>
                <TableRow>
                  <TableCell>
                    Scope
                  </TableCell>
                  <TableCell>
                    Entity Name
                  </TableCell>
                  <TableCell>
                    Schedule Expression
                  </TableCell>
                  <TableCell>
                    Duration
                  </TableCell>
                  <TableCell>
                    Time Zone
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {mergedData.map(maintenance => (
                  <ScheduledMaintenanceTableItem key={maintenance.id} maintenance={maintenance}/>
                ))}
              </TableBody>
            </ThemedTable>
          ) : (
            <div className='ds-u-margin-top--1' css={getEmptyTableMessageStyle(theme)}>
              There are no scheduled maintenance windows
            </div>
          )}
        </div>
      </div>
    </>

  )
}

export default ScheduledMaintenanceTable;
