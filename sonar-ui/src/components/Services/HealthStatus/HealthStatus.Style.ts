import { Theme, css } from '@emotion/react';

export function getChartsTablePropertiesStyle(theme: Theme) {
  return css({
    width: '100%',

    "table, th, td": {
      color: theme.textColor,
      border: "none"
    },

    "th, td": {
      background: theme.foregroundColor,
      padding: "0.5em"
    },

    td: {
      padding: "0.3em"
    }
  });
}

export const TextAlignCenter = css({
  "text-align": "center"
});
