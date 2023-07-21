import { TableBody, TableCell, TableHead, TableRow } from "@cmsgov/design-system";
import UserCell from "./UserPermissionsTableUserCell";
import ThemedTable from "components/Common/ThemedTable";
import { userPermsTableData } from "./test-data"
import DeleteIcon from "components/Icons/DeleteIcon";
import GhostActionButton from "components/Common/GhostActionButton";

export interface UserPermissions {
  name: string,
  email: string,
  tenant: string,
  role: string
}

const UserPermissionsTable = () => {
  const tableRows = userPermsTableData.map(row => (
    <TableRow key={`${row.email}:${row.tenant}:${row.role}`}>
      <UserCell name={row.name} email={row.email} />
      <TableCell>{row.role}</TableCell>
      <TableCell>{row.tenant}</TableCell>
      <TableCell align="center">
        <GhostActionButton>
          <DeleteIcon /> <strong>Delete</strong>
        </GhostActionButton>
      </TableCell>
    </TableRow>
  ));

  return (
    <div className='ds-l-row'>
      <div className='ds-l-col--12'>
        <ThemedTable>
          <TableHead>
            <TableRow>
              <TableCell>User</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Tenant</TableCell>
              <TableCell></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {tableRows}
          </TableBody>
        </ThemedTable>
      </div>
    </div>
  );
};

export default UserPermissionsTable;
