import { Button, ButtonProps } from '@cmsgov/design-system';
import { secondaryActionButtonStyle } from './SecondaryActionButton.Style';

const SecondaryActionButton = (props: ButtonProps) => {
  return (
    <Button {...props} css={secondaryActionButtonStyle}>
      {props.children}
    </Button>
  );
};

export default SecondaryActionButton;
