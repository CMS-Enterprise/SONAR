import { css, SerializedStyles, Theme } from '@emotion/react';
import { StaticTextFontStyle } from '../../App.Style';

export const GetHeaderLabelStyle: (theme: Theme) => SerializedStyles = theme => css({
  ...StaticTextFontStyle,
  cursor: 'pointer',
  fontSize: '2rem',
})

export const getCreateButtonStyle : (theme: Theme) => SerializedStyles = theme => css({
  backgroundColor: theme.accentColor,
  color: theme.backgroundColor
});
