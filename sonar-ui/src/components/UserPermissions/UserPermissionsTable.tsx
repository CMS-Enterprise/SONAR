import { useState } from 'react';
import {
  ArrowIcon,
  ArrowsStackedIcon,
  TableBody,
  TableCell,
  TableHead,
  TableRow
} from "@cmsgov/design-system";
import UserCell from "./UserPermissionsTableUserCell";
import ThemedTable from "components/Common/ThemedTable";
import DeleteIcon from "components/Icons/DeleteIcon";
import { Outlet, useLocation, useOutletContext, useParams } from "react-router";
import { OutletContextType as UserPermissionsOutletContextType } from "pages/UserPermissions";
import { useUserContext } from "components/AppContext/AppContextProvider";
import { Link } from "react-router-dom";
import { PermissionConfiguration } from 'api/data-contracts';
import {getEmptyTableMessageStyle} from '../Common/ThemedTableStyle';
import { useTheme } from '@emotion/react';

enum ColumnName {
  User = 'User',
  Role = 'Role',
  Tenant = 'Tenant'
}

interface SortParams {
  column: ColumnName;
  ascending: boolean;
}

const UserPermissionsTable = () => {
  const theme = useTheme();
  const params = useParams();
  const location = useLocation();
  const context = useOutletContext<UserPermissionsOutletContextType>();
  const { userInfo } = useUserContext();
  const [ sortParams, setSortParams ] = useState<SortParams>({column: ColumnName.User, ascending: true});

  const handleChangeSort = (column: ColumnName) => {
    column === sortParams.column
      ? setSortParams({column: sortParams.column, ascending: !sortParams.ascending})
      : setSortParams({column, ascending: true});
  }

  const sortComparator = (a: PermissionConfiguration, b: PermissionConfiguration) => {
    [ a, b ] = sortParams.ascending ? [ a, b ] : [ b, a ];

    let [ _a, _b ] = [ '', '' ];

    switch (sortParams.column) {
      case ColumnName.Role:
        [ _a, _b ] = [ a.permission!, b.permission! ];
        break;
      case ColumnName.User:
        [ _a, _b ] = [ context.usersByEmail[a.userEmail!], context.usersByEmail[b.userEmail!] ];
        break;
      case ColumnName.Tenant:
        [ _a, _b ] = [ a.tenant || _a , b.tenant || _b ];
        break;
    }

    return _a.localeCompare(_b);
  };

  const tableRows = context.permConfigByEnv[params.environmentName!]?.sort(sortComparator).map(p => (
    <TableRow key={`${p.id}`}>
    <UserCell name={context.usersByEmail[p.userEmail!]} email={p.userEmail!} />
    <TableCell>{p.permission}</TableCell>
    <TableCell>{p.tenant || <small><i>(All Tenants)</i></small>}</TableCell>
    <TableCell align="right" className="ds-u-valign--middle">
      {
        p.userEmail !== userInfo?.email ? (
          <span>
            <Link to={`${p.id}/delete`} replace={true} state={{ from: location.pathname }}>
              <DeleteIcon className='ds-u-font-size--sm' />&nbsp;<b>Delete</b>
            </Link>
          </span>
        ) : (
          <small>(You)</small>
        )
      }
    </TableCell>
  </TableRow>
  ));

  const sortableTableCell = (column: ColumnName, columnSpan: number) => (
    <TableCell className={`ds-l-col--${columnSpan} sortable`} onClick={() => handleChangeSort(column)}>
      {column}
      {
        sortParams.column === column
          ? sortParams.ascending
            ? <ArrowIcon direction='up' />
            : <ArrowIcon direction='down' />
          : <ArrowsStackedIcon />
      }
    </TableCell>
  );

  return (
    tableRows ? (
      <div className='ds-l-row'>
        <div className='ds-l-col--12'>
          <ThemedTable>
            <TableHead>
              <TableRow>
                {sortableTableCell(ColumnName.User, 4)}
                {sortableTableCell(ColumnName.Role, 2)}
                {sortableTableCell(ColumnName.Tenant, 4)}
                <TableCell align="right" className="ds-l-col--2" />
              </TableRow>
            </TableHead>
            <TableBody>
              {tableRows}
            </TableBody>
          </ThemedTable>
        </div>
        <Outlet context={context}/>
      </div>
    ) : (
      <div className="ds-l-row ds-u-margin-top--1" css={getEmptyTableMessageStyle(theme)}>
        <div className="ds-l-col--12">
          There are no user permissions associated with your account in the {params.environmentName} environment.
        </div>
      </div>
    )
  );
};

export default UserPermissionsTable;
