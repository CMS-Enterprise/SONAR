import { Button, ButtonProps } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { expandableRowButtonStyle } from './ExpandableRowButton.Style';

export const ExpandableRowButton = (props: ButtonProps) => {
  const theme = useTheme();

  return (
    <Button size='small' variation='solid' css={expandableRowButtonStyle(theme)} {...props}>
      {props.children}
    </Button>
  );
};

export default ExpandableRowButton;
