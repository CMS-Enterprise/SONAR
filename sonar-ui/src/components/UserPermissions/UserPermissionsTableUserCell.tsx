import { TableCell } from "@cmsgov/design-system";
import { userCellStyle } from "./UserPermissionsTableUserCell.Style";

interface Props {
  name: string;
  email: string;
}

const UserPermissionsTableUserCell = (props: Props) => {
  return (
    <TableCell>
      <div css={userCellStyle}>
        <p>{props.name}</p>
        <small>{props.email}</small>
      </div>
    </TableCell>
  );
};

export default UserPermissionsTableUserCell;
