import { AlertVariation } from '@cmsgov/design-system/dist/types/Alert/Alert';
import { createContext, JSX, ReactNode, useContext, useState } from 'react';
import { v4 as uuidv4 } from 'uuid';

interface UiAlert {
  id: string,
  alertHeader: string,
  alertBody: string,
  alertType: AlertVariation,
  sourceKey?: string
}

interface AlertContextType {
  alerts: UiAlert[],
  createAlert: (header: string, body: string, type: AlertVariation, sourceKey?: string) => void
  deleteAlertById: (id: string) => void,
}

const AlertContext =
  createContext<AlertContextType | null>(null);

export const useAlertContext = () => useContext(AlertContext)!;
export default function AlertContextProvider({ children }: { children: ReactNode }): JSX.Element {
  const [alerts, setAlerts] = useState<UiAlert[]>([]);

  // createAlert method that adds a new alert to the state array (max 5 alerts).
  const createAlert = (header: string, body: string, type: AlertVariation, sourceKey: string | undefined) => {
    // duplicate alert coming from same operation, do nothing
    if (sourceKey && alerts.some(a => a.sourceKey === sourceKey)) {
      return;
    }

    const newAlert = {id: uuidv4(), alertHeader: header, alertBody: body, alertType: type, sourceKey: sourceKey};
    setAlerts(
      alerts.length + 1 > 5 ?
        [...alerts.slice(1), newAlert] :
        [...alerts, newAlert]
    )
  }

  const deleteAlertById = (id: string) => {
    setAlerts(
      alerts.filter(a => a.id !== id)
    );
  }

  const alertContextValue: AlertContextType = {
    alerts: alerts,
    createAlert: createAlert,
    deleteAlertById: deleteAlertById
  }

  return (
    <AlertContext.Provider value={alertContextValue}>
      {children}
    </AlertContext.Provider>
  );
}
