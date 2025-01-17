import { css, Theme } from '@emotion/react';

export function getDrawerStyle(theme: Theme) {
  return css({
    color: theme.textColor,

    "--drawer__background-color": theme.foregroundColor,
    "--drawer-header__background-color": theme.foregroundColor,
    "--drawer__border-color": theme.textColor,
    "--button__border-color": theme.accentColor,

    "boxShadow": `-1px 0 0 ${theme.textColor}`,

    ".ds-c-drawer__header": {
      "borderBottom": `3px solid ${theme.accentColor}`,
      "marginLeft": "1em",
      "marginRight": "1em",
      "padding": "1em 0 0.5em 0"
    },

    // The heading level of this child selector must correspond to the
    // `headerLevel` property of the Drawer this style is applied to.
    ".ds-c-drawer__header h3": {
      "paddingTop": "1em"
    },

    ".ds-c-button": {
      backgroundColor: theme.backgroundColor,
      color: theme.accentColor
    },

    ".ds-c-button:hover": {
      backgroundColor: theme.accentColor,
      color: theme.foregroundColor
    }
  });
}

export function getDrawerSectionHeaderStyle(theme: Theme) {
  return css({
    color: theme.textColor,
    borderBottom: "1px solid " + theme.textColor,
  });
}
