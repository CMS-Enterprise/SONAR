import { css, Theme } from '@emotion/react';

export function getBreadcrumbsStyle(theme: Theme) {
  return css({
    borderBottom: "3px solid " + theme.accentColor,
    padding: "10px 10px 5px 10px",
    flexWrap: "wrap",
    height: "auto",
    wordBreak: "break-all"
  });
}

export const crumbStyle = css({
  fontSize: 20,
  fontWeight: 400,
  marginBottom: 20
})

export const crumbDelimiterStyle = css({
  "> :not(:first-of-type):before": {
    content: '"/"',
    marginLeft: 5,
    marginRight: 5
  },
  ":last-child:after": {
    display:"none",
  }
})
