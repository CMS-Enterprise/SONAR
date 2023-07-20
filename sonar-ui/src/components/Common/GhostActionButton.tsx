import { Button, ButtonProps } from "@cmsgov/design-system";
import { useTheme } from "@emotion/react";
import * as styles from '../App/Header.Style';
import { getGhostActionButtonStyle } from "./GhostActionButton.Style";

const GhostActionButton = (props: ButtonProps) => {
  const theme = useTheme();

  return (
    <Button {...props} css={getGhostActionButtonStyle(theme)} size={'small'}>
      {props.children}
    </Button>
  );
};

export default GhostActionButton;
