import { StarIcon, TableBody, TableCell, TableHead, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import React, { useContext } from 'react';
import { HealthStatus } from '../../../../api/data-contracts';
import {
  ArgoCheckType,
  IArgoAppHealthStatusCheckDefinition, IArgoAppSyncStatusCheckDefinition,
  IArgoHealthCheckDefinition
} from '../../../../types';
import HealthStatusBadge from '../../../Badges/HealthStatusBadge';
import ThemedTable from '../../../Common/ThemedTable';
import ExternalLinkIcon from '../../../Icons/ExternalLinkIcon';
import { ServiceOverviewContext } from '../../ServiceOverviewContext';
import { getArgoStatusIndicatorIconStyle } from './ArgoHealthCheck.Style';

const ArgoHealthCheckDetails: React.FC<{
  healthCheckStatus: HealthStatus | null
}> = ({ healthCheckStatus }) => {
  const theme = useTheme();
  const context = useContext(ServiceOverviewContext)!;
  const healthCheck = context.selectedHealthCheck!;
  const baseDefinition = healthCheck.definition as IArgoHealthCheckDefinition;

  const typedMappings = baseDefinition.checkType === ArgoCheckType[ArgoCheckType.HealthStatus] ?
   (baseDefinition as IArgoAppHealthStatusCheckDefinition).healthStatusMappings :
    (baseDefinition as IArgoAppSyncStatusCheckDefinition).syncStatusMappings;

  // convert current health check status to argo status
  const mappedArgoStatus = (Object.keys(typedMappings) as Array<string>)
    .find(key => typedMappings[key] === healthCheckStatus);

  return (
    <>
      <div>
        <p>
          <b>
            Application Name:&nbsp;
          </b>
          {baseDefinition.applicationName}
        </p>
      </div>
      <div>
        <p>
          <b>Argo Dashboard Url: </b>
          <a target="_blank" rel="noreferrer" href={baseDefinition.argoDashboardUrl}>
            {baseDefinition.argoDashboardUrl}&nbsp;
            <ExternalLinkIcon className="ds-u-font-size--sm ds-u-valign--top"/>
          </a>
        </p>
      </div>
      <div>
        <p>
          <b>
            Argo Check
            Type:&nbsp;
          </b>
          {
            baseDefinition.checkType === ArgoCheckType[ArgoCheckType.HealthStatus] ?
              'Application Health Status' :
              'Application Sync Status'
          }
        </p>
      </div>

      {mappedArgoStatus ? (
        <div>
          <p>
            <b>
              Argo {baseDefinition.checkType === ArgoCheckType[ArgoCheckType.HealthStatus] ?
                'Health Status' :
                'Sync Status'}:&nbsp;
            </b>
            {mappedArgoStatus}
          </p>
        </div>
      ) : null}

      <ThemedTable>
        <TableHead>
          <TableRow>
            <TableCell css={{width: '1%'}}/>
            <TableCell css={{width: '5%'}}>
              Sonar Status
            </TableCell>
            <TableCell css={{width: '20%'}}>
              Argo Status
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {Object.entries(typedMappings).map(([key, value]) => {
            const sonarStatus = value as HealthStatus;
            return (
              <TableRow key={`${key}-${value}`}>
                <TableCell>
                  {
                    mappedArgoStatus === key ? (
                      <StarIcon
                        isFilled={true}
                        css={getArgoStatusIndicatorIconStyle(theme, sonarStatus)}
                      />
                    ) : null
                  }
                </TableCell>
                <TableCell>
                  <HealthStatusBadge theme={theme} status={sonarStatus}/>
                </TableCell>
                <TableCell>
                  {key}
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </ThemedTable>
    </>
  )
}

export default ArgoHealthCheckDetails;
