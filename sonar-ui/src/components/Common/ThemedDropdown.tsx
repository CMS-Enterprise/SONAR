import { Dropdown } from '@cmsgov/design-system';
import { BaseDropdownProps } from '@cmsgov/design-system/dist/types/Dropdown/Dropdown';
import { useTheme } from '@emotion/react';
import { getDropdownStyle } from './ThemedDropdown.Style';

const ThemedDropdown = (props: BaseDropdownProps) => {
  const theme = useTheme();

  return (
    <Dropdown
      css={getDropdownStyle(theme)}
      {...props}
    />
  )
}

export default ThemedDropdown;
