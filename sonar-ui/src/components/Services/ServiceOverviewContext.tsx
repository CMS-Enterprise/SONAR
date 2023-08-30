import { HealthCheckModel, ServiceConfiguration, ServiceHierarchyConfiguration, ServiceHierarchyHealth } from "api/data-contracts";
import { createContext } from "react";

interface ServiceOverviewContextType {
  environmentName: string,
  tenantName: string,
  serviceConfiguration: ServiceConfiguration,
  serviceHierarchyConfiguration: ServiceHierarchyConfiguration,
  serviceHierarchyHealth: ServiceHierarchyHealth,
  selectedHealthCheck: HealthCheckModel | null | undefined,
  setSelectedHealthCheck: React.Dispatch<React.SetStateAction<HealthCheckModel | null | undefined>>
}

export const ServiceOverviewContext = createContext<ServiceOverviewContextType | null | undefined>(null);
