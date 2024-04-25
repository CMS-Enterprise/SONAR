import { Spinner, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React, { useState } from 'react';
import {getEmptyTableMessageStyle} from '../Common/ThemedTableStyle';
import ThemedModalDialog from '../Common/ThemedModalDialog';
import ThemedTable from '../Common/ThemedTable';
import AdHocMaintenanceHeader from './AdHocMaintenanceHeader';
import AdhocMaintenanceTableItem from './AdhocMaintenanceTableItem';
import CreateMaintenanceForm from './CreateMaintenanceForm';
import { useGetActiveAdHocMaintenances } from './Maintenance.Hooks';

const AdHocMaintenanceTable = () => {
  const theme = useTheme();
  const [open, setOpen] = useState(false);
  const handleModalToggle = () => {
    setOpen(!open);
  }

  const data = useGetActiveAdHocMaintenances();
  const isLoading = data.some(e => e.isLoading);
  const mergedData = data.flatMap(e => e.data ?? []);
  return (
    <>
      <AdHocMaintenanceHeader handleModalToggle={handleModalToggle}/>
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
                    Applied By
                  </TableCell>
                  <TableCell>
                    Start Time
                  </TableCell>
                  <TableCell>
                    End Time
                  </TableCell>
                  <TableCell/>
                </TableRow>
              </TableHead>
              <TableBody>
                {mergedData.map(maintenance => (
                  <AdhocMaintenanceTableItem key={maintenance.id} maintenance={maintenance}/>
                ))}
              </TableBody>
            </ThemedTable>
          ) : (
            <div className='ds-u-margin-top--1' css={getEmptyTableMessageStyle(theme)}>
              There are no active ad-hoc maintenances
            </div>
          )}
        </div>
      </div>
      {open ?
        <ThemedModalDialog
          heading={'Start Maintenance Window'}
          onExit={handleModalToggle}
          onClose={handleModalToggle}
          actions={
            <CreateMaintenanceForm
              handleModalToggle={handleModalToggle}
            />
          }
        /> : null
      }
    </>

)
}

export default AdHocMaintenanceTable;
