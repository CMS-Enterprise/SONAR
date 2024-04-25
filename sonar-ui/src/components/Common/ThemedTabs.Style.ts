import { css, SerializedStyles, Theme } from '@emotion/react';

export const getTabsStyle : (theme: Theme) => SerializedStyles = theme => css({
  'a': {
    backgroundColor: theme.backgroundColor,
    '&.ds-c-tabs__item': {
      backgroundColor: theme.foregroundColor,
      color: theme.textColor,
    },
    '&.ds-c-tabs__item:visited[aria-selected=true]': {
      color: theme.accentColor,
      borderBottom: 'none'
    },
    '&.ds-c-tabs__item:after': {
      backgroundColor: theme.accentColor,
      borderTop: `2px ${theme.accentColor} solid`,
      marginTop: -1
    },
    '&.ds-c-tabs__item:hover': {
      color: theme.accentColor
    }
  },
  'div': {
    '&.ds-c-tabs__panel': {
      backgroundColor: theme.foregroundColor,
      border: 'none'
    },
    '&.ds-c-tabs': {
      border: 'none'
    }
  }

});
