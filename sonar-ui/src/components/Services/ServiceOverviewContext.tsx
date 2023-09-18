import { HealthCheckModel, ServiceConfiguration, ServiceHierarchyConfiguration, ServiceHierarchyHealth, ServiceVersionDetails} from 'api/data-contracts';
import { createContext } from "react";

interface ServiceOverviewContextType {
  environmentName: string,
  tenantName: string,
  serviceConfiguration: ServiceConfiguration,
  serviceHierarchyConfiguration: ServiceHierarchyConfiguration,
  serviceHierarchyHealth: ServiceHierarchyHealth,
  serviceVersionDetails: ServiceVersionDetails[],
  selectedHealthCheck: HealthCheckModel | null | undefined,
  setSelectedHealthCheck: React.Dispatch<React.SetStateAction<HealthCheckModel | null | undefined>>
}

export const ServiceOverviewContext = createContext<ServiceOverviewContextType | null | undefined>(null);
