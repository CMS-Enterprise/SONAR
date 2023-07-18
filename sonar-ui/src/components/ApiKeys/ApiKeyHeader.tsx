import { Button } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import { GetHeaderLabelStyle } from './ApiKeyHeader.Style';

const ApiKeyHeader = () => {
  const theme = useTheme();
  return (
    <div className="ds-l-row ds-u-justify-content--end">
      <div
        className="ds-l-col--4 ds-u-margin-right--auto ds-u-margin-left--0"
        css={GetHeaderLabelStyle(theme)}
      >
        Your Api Keys
      </div>
      <div
        className="ds-l-col--2 ds-u-margin-right--0 ds-u-margin-left--auto ds-u-text-align--right"
      >
        <Button variation={"solid"}>+ Create API Key</Button>
      </div>
    </div>
  )
}

export default ApiKeyHeader;
