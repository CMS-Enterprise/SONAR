import { TextField } from '@cmsgov/design-system';
import { BaseTextFieldProps } from '@cmsgov/design-system/dist/types/TextField/TextField';
import { useTheme } from '@emotion/react';
import { getTextFieldStyle } from './ThemedTextField.Style';

const ThemedTextField = (props: BaseTextFieldProps) => {
  const theme = useTheme();

  return (
    <TextField
      css={getTextFieldStyle(theme)}
      {...props} />
  )
}

export default ThemedTextField;
