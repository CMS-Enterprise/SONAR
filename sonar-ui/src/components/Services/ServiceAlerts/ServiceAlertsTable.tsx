import { TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { ServiceAlert } from '../../../api/data-contracts';
import {getEmptyTableMessageStyle} from '../../Common/ThemedTableStyle';
import ThemedTable from '../../Common/ThemedTable';
import ServiceAlertTableItem from './ServiceAlertTableItem';

const ServiceAlertsTable: React.FC<{
  alerts: ServiceAlert[]
}> = ({ alerts }) => {
  const theme = useTheme();
  return (
    <div className='ds-l-row ds-u-align-items--start'>
      <div className='ds-l-col--12'>
        {alerts.length > 0 ? (
          <ThemedTable>
            <TableHead>
              <TableRow>
                <TableCell width={"20%"}>
                  Status
                </TableCell>
                <TableCell>
                  Name
                </TableCell>
                <TableCell>
                  Threshold
                </TableCell>
                <TableCell>
                  Receiver
                </TableCell>
                <TableCell>
                  Notifications
                </TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {alerts.map(alert => (
                <ServiceAlertTableItem alert={alert} key={alert.name} />
              ))}
            </TableBody>
          </ThemedTable>
        ) : (
          <div className='ds-u-margin-top--1' css={getEmptyTableMessageStyle(theme)}>
            There are no Alerts at this time
          </div>
        )}
      </div>
    </div>
  );
};

export default ServiceAlertsTable;
