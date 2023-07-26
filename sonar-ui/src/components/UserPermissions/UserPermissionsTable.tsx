import { TableBody, TableCell, TableHead, TableRow } from "@cmsgov/design-system";
import UserCell from "./UserPermissionsTableUserCell";
import ThemedTable from "components/Common/ThemedTable";
import DeleteIcon from "components/Icons/DeleteIcon";
import GhostActionButton from "components/Common/GhostActionButton";
import { useOutletContext, useParams } from "react-router";
import { PermissionConfigurationByEnvironment } from "pages/UserPermissions.Hooks";

const UserPermissionsTable = () => {
  const { environmentName } = useParams();
  const permConfigByEnv = useOutletContext<PermissionConfigurationByEnvironment>();
  const envPermConfig = permConfigByEnv[environmentName!];

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
            {
              envPermConfig.map(row => (
                <TableRow key={`${row.userEmail}:${row.tenant}:${row.permission}`}>
                  <UserCell name={row.id!} email={row.userEmail!} />
                  <TableCell>{row.permission}</TableCell>
                  <TableCell>{row.tenant}</TableCell>
                  <TableCell align="center">
                    <GhostActionButton>
                      <DeleteIcon /> <strong>Delete</strong>
                    </GhostActionButton>
                  </TableCell>
                </TableRow>
              ))
            }
          </TableBody>
        </ThemedTable>
      </div>
    </div>
  );
};

export default UserPermissionsTable;
