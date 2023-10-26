import { css, SerializedStyles, Theme } from '@emotion/react';
export const getSearchInputStyle : (theme: Theme) => SerializedStyles = theme => css({
  borderRadius: ".4375rem",
  border: '1px solid #5A5A5A',
  backgroundColor: theme.foregroundColor,
  color: theme.textColor,
  width: "100%",
  fontSize: 16,
  padding: 8,
  lineHeight: 1.3
});

export const getSearchInputContainerStyle = css({
  marginTop: 24,
  marginBottom: 6
})
export const getCloseFilterButtonStyle : (theme: Theme) => SerializedStyles = theme => css({
  textAlign: "center",
  padding: "8px 8px",
  "svg": {
    height: ".75em"
  }
})
