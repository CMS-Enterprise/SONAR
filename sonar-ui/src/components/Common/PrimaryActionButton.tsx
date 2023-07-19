import { Button, ButtonProps } from "@cmsgov/design-system";
import { useTheme } from "@emotion/react";
import { primaryActionButtonStyle } from "./PrimaryActionButton.Style";

const PrimaryActionButton = (props: ButtonProps) => {
  const theme = useTheme();

  return (
    <Button {...props} css={primaryActionButtonStyle(theme)}>
      {props.children}
    </Button>
  );
};

export default PrimaryActionButton;
