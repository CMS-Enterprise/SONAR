import { Badge } from '@cmsgov/design-system';
import { Theme } from '@emotion/react';
import React from 'react';
import { HealthStatus } from '../../api/data-contracts';
import { badgeStyle } from './HealthStatusBadge.Style';

const HealthStatusBadge: React.FC<{
  theme: Theme,
  status: HealthStatus | undefined
}> =
  ({ theme, status}) => {
    return (
      <Badge
        children={HealthStatus[status ? status : HealthStatus.Unknown]}
        size="big"
        css={badgeStyle(theme, status)}
      />
    );
  };

export default HealthStatusBadge;
