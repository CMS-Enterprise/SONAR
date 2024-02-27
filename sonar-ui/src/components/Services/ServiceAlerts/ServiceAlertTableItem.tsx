import { AlertCircleIcon, CheckCircleIcon, TableCell, TableRow } from '@cmsgov/design-system';
import { useTheme } from '@emotion/react';
import dayjs from 'dayjs';
import React, { useState } from 'react';
import { ServiceAlert } from '../../../api/data-contracts';
import { useUserContext } from '../../AppContext/AppContextProvider';
import GhostActionButton from '../../Common/GhostActionButton';
import SilenceIcon from '../../Icons/SilenceIcon';
import UnsilenceIcon from '../../Icons/UnsilenceIcon';
import { getAlertIconStyle, SilenceIconStyle } from './ServiceAlerts.Style';
import SilenceAlertModal from './SilenceAlertModal';

const ServiceAlertTableItem: React.FC<{
  alert: ServiceAlert
}> = ({ alert }) => {

  const theme = useTheme();
  const [modalOpen, setModalOpen] = useState(false);
  const { userIsAuthenticated} = useUserContext();

  const handleModalToggle = () => {
    setModalOpen(!modalOpen);
  }

  return (
    <TableRow key={alert.name}>
      <TableCell>
        {alert.isFiring ? (
          <>
            <AlertCircleIcon css={getAlertIconStyle(theme, alert.isFiring)} />
            Firing since {dayjs(alert.since).format("MM/DD/YYYY HH:mm:ss")}
          </>
        ) : (
          <>
            <CheckCircleIcon css={getAlertIconStyle(theme, alert.isFiring)} /> Ok
          </>
        )}
      </TableCell>
      <TableCell>
        {alert.name}
      </TableCell>
      <TableCell>
        {alert.threshold}
      </TableCell>
      <TableCell>
        {alert.receiverName} ({alert.receiverType})
      </TableCell>
      <TableCell>
        {alert.isSilenced ? (
          <>
            Silenced until {dayjs(alert.silenceDetails?.endsAt).format("MM/DD/YYYY HH:mm:ss")} by {alert.silenceDetails?.silencedBy}
          </>
        ) : (
          <>
            On
          </>
        )}
      </TableCell>
      <TableCell>
        {userIsAuthenticated ? (
          <GhostActionButton onClick={handleModalToggle}>
            {alert.isSilenced ? (
              <>
                <UnsilenceIcon css={SilenceIconStyle} /> Unsilence
              </>
            ) : (
              <>
                <SilenceIcon css={SilenceIconStyle} /> Silence
              </>
            )}
          </GhostActionButton>
        ) : null}
        {modalOpen ?
          <SilenceAlertModal
            alert={alert}
            handleModalToggle={handleModalToggle}
          /> :
          null
        }
      </TableCell>
    </TableRow>
  )
}

export default ServiceAlertTableItem;
