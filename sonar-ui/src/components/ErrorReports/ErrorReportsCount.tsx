import { WarningIcon } from '@cmsgov/design-system';
import React from 'react';
import { Link } from 'react-router-dom';
import { errorReportsCountStyle } from 'components/ErrorReports/ErrorReportsCount.Style';
import { useUserContext } from 'components/AppContext/AppContextProvider';

const ErrorReportsCount: React.FC<{
  errorReportsPath: string,
  errorReportsCount: number
}> = ({errorReportsPath, errorReportsCount}) => {
  const { userIsAuthenticated, userInfo } = useUserContext();

  return (
    <>
      { (userIsAuthenticated && userInfo?.isAdmin) ?
        <span title='View Error Reports'>
          <Link to={`/error-reports/environments${errorReportsPath}`} css={errorReportsCountStyle}>
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<WarningIcon className='ds-u-font-size--sm' />&nbsp;({errorReportsCount})
          </Link>
        </span> :
        null
      }
    </>
  )
};

export default ErrorReportsCount;
