import { TableBody, TableCell, TableHead, TableRow } from "@cmsgov/design-system";
import { Link, useOutletContext } from "react-router-dom";
import PeopleIcon from "components/Icons/PeopleIcon";
import ThemedTable from "components/Common/ThemedTable";
import { OutletContextType as UserPermissionsOutletContextType } from "pages/UserPermissions";
import {getEmptyTableMessageStyle} from '../Common/ThemedTableStyle';
import { useTheme } from "@emotion/react";

const EnvironmentUsersTable = () => {
  const theme = useTheme();
  const context = useOutletContext<UserPermissionsOutletContextType>();

  const tableRows = Object.keys(context.permConfigByEnv).map(envName => (
    <TableRow key={envName}>
    <TableCell>
      <Link to={`environments/${envName}`}>{envName}</Link>
    </TableCell>
    <TableCell align="right">
      <PeopleIcon />&nbsp;&nbsp;{context.permConfigByEnv[envName].length}
    </TableCell>
  </TableRow>
  ));

  return (
    tableRows ? (
      <div className='ds-l-row'>
        <div className='ds-l-col--12'>
          <ThemedTable>
            <TableHead>
              <TableRow>
                <TableCell>Environment</TableCell>
                <TableCell align="right"># Users</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {tableRows}
            </TableBody>
          </ThemedTable>
        </div>
      </div>
    ) : (
      <div className="ds-l-row ds-u-margin-top--1" css={getEmptyTableMessageStyle(theme)}>
        <div className="ds-l-col--12">
          There are no user permissions associated with your account in any environments.
        </div>
      </div>
    )
  );
}

export default EnvironmentUsersTable;
