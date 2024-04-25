import { css, SerializedStyles, Theme } from '@emotion/react';
export const getDatePickerStyle : (theme: Theme) => SerializedStyles = theme => css({
  width: '100% !important',
  borderRadius: ".4375rem",
  border: '1px solid #5A5A5A',
  backgroundColor: theme.backgroundColor,
  color: theme.textColor,
  whiteSpace: "pre-wrap",
  padding: 10


});

export const DatePickerContainerStyle = css({
  '& .flatpickr-wrapper': {
    width: '100% !important',
    margin: '4px 0px'
  },
  '& label': {
    marginTop: 12,
    whiteSpace: "pre-wrap"
  }
})
