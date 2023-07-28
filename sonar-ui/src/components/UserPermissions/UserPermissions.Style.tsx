import { Theme, css } from "@emotion/react"

export const getEmptyTableMessageStyle = (theme: Theme) => {
  return css({
    backgroundColor: theme.foregroundColor,
    borderRadius: "7px",
    margin: "0 -10px",
    padding: "0.625rem 13rem",
    color: theme.accentColor,
    textAlign: "center"
  });
}
