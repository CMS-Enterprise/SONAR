import { Table } from "@cmsgov/design-system";
import { useTheme } from "@emotion/react";
import { getTableContainerStyle, getTableStyle } from "./ThemedTableStyle";
import { TableProps } from "@cmsgov/design-system/dist/types/Table/Table";

const ThemedTable = (props: TableProps) => {
  const theme = useTheme();

  return (
    <div className='ds-u-margin-top--1' css={getTableContainerStyle}>
      <Table borderless css={getTableStyle(theme)}>
        {props.children}
      </Table>
    </div>
  );
}

export default ThemedTable;
