import { css } from '@emotion/react';

export function getChartsFlexContainerStyle() {
  return css({
    display:'flex',
    justifyContent:'left',
    overflow: 'hidden'
  });
}

export function getChartsFlexTableStyle() {
  return css({
    flexGrow:'0',
    flexShrink:'0',
    margin: '10px',
    maxHeight: '300px',
    overflow: 'scroll',
  });
}

export function getChartsFlexThresholdStyle() {
  return css({
    flexGrow:'0',
    flexShrink:'1',
    margin: '10px'
  });
}

export function getChartsTablePropertiesStyle() {
  return css({
    width: '100%'
  });
}
