import { TableBody, TableCell, TableHead, TableRow } from "@cmsgov/design-system";
import { envsTableData } from "./test-data";
import { Link } from "react-router-dom";
import PeopleIcon from "components/Icons/PeopleIcon";
import ThemedTable from "components/Common/ThemedTable";

export interface EnvironmentUsers {
  environmentName: string;
  numUsers: number;
}

interface Props {
  data?: EnvironmentUsers[];
}

const EnvironmentUsersTable = (props: Props) => {
  const tableRows = envsTableData.map(row => (
    <TableRow key={row.environmentName}>
      <TableCell>
        <Link to={`environments/${row.environmentName}`}>{row.environmentName}</Link>
      </TableCell>
      <TableCell align="right">
        <PeopleIcon />&nbsp;&nbsp;{row.numUsers}
      </TableCell>
    </TableRow>
  ));

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
            {tableRows}
          </TableBody>
        </ThemedTable>
      </div>
    </div>
  );
}

export default EnvironmentUsersTable;
