import { AddIcon, Button } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React from 'react';
import { getFabStyle } from './ThemedFab.Style';

const ThemedFab: React.FC<{
  action: () => void
}> = ({ action }) => {
  const theme = useTheme();
  return (
    <Button
      onClick={action}
      css={getFabStyle(theme)}
    >
      <AddIcon />
    </Button>
  )
}

export default ThemedFab;
