import { Button, ButtonProps } from "@cmsgov/design-system";
import { useTheme } from "@emotion/react";
import { getGhostActionButtonStyle } from "./GhostActionButton.Style";

const GhostActionButton = (props: ButtonProps) => {
  const theme = useTheme();

  return (
    <Button size={'small'} css={getGhostActionButtonStyle(theme)} {...props}>
      {props.children}
    </Button>
  );
};

export default GhostActionButton;
