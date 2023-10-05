import { css, Theme } from '@emotion/react';

export function getEnvironmentStatusStyle(theme: Theme) {
  return css({
    backgroundColor: theme.foregroundColor,
    "--accordion__background-color": "none",
    "--accordion-content__background-color": "none",
    "--accordion__background-color--hover": theme.highlightColor,
    boxShadow: "rgba(50, 50, 93, 0.25) 0px 30px 60px -12px, rgba(0, 0, 0, 0.3) 0px 18px 36px -18px",
    borderRadius: 40,
    padding: "15px 15px 15px 15px",
    "& .ds-c-accordion__button": {
      color: theme.textColor,
      fontSize: "22px",
      fontWeight: "600",
      padding: "3px 20px 2px 20px",
      borderRadius: 40,
      ":focus": {
        boxShadow: "none"
      },
      ":hover": {
        color: theme.accentColor
      }
    },
    "& .ds-c-accordion__content": {
      border: "none",
      fontSize: "15px",
      fontWeight: "400",
      padding: "0px 20px 5px 20px",
      borderRadius: 40,
    },
    "& .ds-c-accordion__content h3": {
      marginTop: "15px",
      marginBottom: "10px",
    }
  });
}

export const EnvironmentItemContainerStyle = css({
  marginTop: 5,
  marginBottom: 10,
})

export function getAccordionToggleStyle(theme: Theme) {
  return css({
    textAlign: "right",
    button: {
      color: theme.textColor,
      ":focus": {
        boxShadow: "none",
        "--button-ghost__color--hover": theme.accentColor
      },
      ":hover": {
        "--button-ghost__color--active": theme.accentColor,
        color: theme.accentColor
      }
    }
  })
}
