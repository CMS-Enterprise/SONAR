import { TableBody, TableCell, TableHead, TableRow } from "@cmsgov/design-system";
import { Link, useOutletContext } from "react-router-dom";
import PeopleIcon from "components/Icons/PeopleIcon";
import ThemedTable from "components/Common/ThemedTable";
import { PermissionConfigurationByEnvironment } from "pages/UserPermissions.Hooks";

const EnvironmentUsersTable = () => {
  const permConfigByEnv = useOutletContext<PermissionConfigurationByEnvironment>();

  return (
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
            {
              Object.keys(permConfigByEnv).map(envName => (
                <TableRow key={envName}>
                  <TableCell>
                    <Link to={`environments/${envName}`}>{envName}</Link>
                  </TableCell>
                  <TableCell align="right">
                    <PeopleIcon />&nbsp;&nbsp;{permConfigByEnv[envName].length}
                  </TableCell>
                </TableRow>
              ))
            }
          </TableBody>
        </ThemedTable>
      </div>
    </div>
  );
}

export default EnvironmentUsersTable;
