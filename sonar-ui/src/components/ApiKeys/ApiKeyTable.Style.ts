import { css, SerializedStyles, Theme } from '@emotion/react';

export const getTableStyle : (theme: Theme) => SerializedStyles = theme => css({
  width: "100%",
  '& td': {
    border: 0,
    borderTop: `1px solid ${theme.textColor}`
  },
  '& th': {
    borderBottom: `1px solid ${theme.accentColor}`
  }
});

export const getTableContainerStyle = (theme: Theme) => {
  return css({
    backgroundColor: theme.foregroundColor,
    paddingLeft: "1.25rem",
    paddingRight: "1.25rem",
    borderRadius: "0.4375rem"
  })
}

export const getEmptyTableMessageStyle = (theme: Theme) => {
  return css({
    padding: "0.625rem 13rem",
    color: theme.accentColor,
    textAlign: "center"
  })
}
