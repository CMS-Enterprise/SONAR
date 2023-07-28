import { SerializedStyles, Theme, css } from "@emotion/react";

export const getTableStyle: (theme: Theme) => SerializedStyles = theme => css({
  width: "100%",

  '& th': {
    borderBottom: `3px solid ${theme.accentColor}`
  },

  '& td': {
    paddingTop: '0.5rem',
    paddingBottom: '0.5rem',
    border: 0,
    borderTop: `1px solid ${theme.highlightColor}`
  },

  '& tbody tr:hover': {
    backgroundColor: theme.highlightColor
  },

  '& th.sortable:hover': {
    backgroundColor: theme.highlightColor,
    color: theme.accentColor,
    cursor: 'pointer'
  },

  '& th.sortable svg': {
    height: '0.75em'
  }
});

export const getTableContainerStyle: (theme: Theme) => SerializedStyles = theme => css({
  backgroundColor: theme.foregroundColor,
  borderRadius: '7px',
  padding: '10px 20px',
  margin: '0 -10px'
});
