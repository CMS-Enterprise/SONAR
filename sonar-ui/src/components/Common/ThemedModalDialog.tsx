import { Dialog, DialogProps } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { getDialogStyle } from './ThemedModalDialog.Style';
const ThemedModalDialog = (props: DialogProps) => {
  const theme = useTheme();

  return (
    <Dialog
      css={getDialogStyle(theme)}
      {...props}
    />
  );


}

export default ThemedModalDialog;
